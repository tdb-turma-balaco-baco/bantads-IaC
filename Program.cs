using System;
using System.Collections.Generic;
using Pulumi;
using Docker = Pulumi.Docker;


return await Deployment.RunAsync((Action)(() =>
{
    var configs = new Config("bantads");

    var rabbitMQImageName = configs.Require("rabbitMQ-image");
    var mongoDbImageName = configs.Require("mongoDb-image");
    var postgresImageName = configs.Require("postgress-image");
    var pgAdminImageName = configs.Require("pgadmin-image");

    var msClientImageName = configs.Require("client-image");
    var msNotificationImage = configs.Require("notification-image");
    var msManagerImage = configs.Require("manager-image");
    var msAccountImage = configs.Require(key: "account-image");
    var msSaga = configs.Require(key: "saga-image");
    var msAuthImage = configs.Require("auth-image");
    var msFrontImage = configs.Require("front-image");
    var msGatewayImage = configs.Require("gateway-image");

    var network = new Docker.Network("network-bantads");

    CreateRabbitMQContainer(rabbitMQImageName, network);

    var postgressDbImage = StackHelper.GetDockerImage(postgresImageName, "PostgreSQL");

    CreateFrontEnd(msFrontImage, network);
    CreateApiGateway(msGatewayImage, network);
    CreateNotificationMicroservice(msNotificationImage, network);
    CreateManagerMicroservice(msManagerImage, postgresImage: postgressDbImage, network);
    CreateAuthMicroservice(mongoDbImageName, msAuthImage, network);
    CreateClientMicroservice(msClientImageName, postgressDbImage, network);
    CreateAccountMicroservice(msAccountImage, postgressDbImage,network);
    CreateSaga(msSaga, network);

    var pgAdminImage = StackHelper.GetDockerImage(pgAdminImageName, "PgAdmin");

     var pgAdmin = new Docker.Container("pgAdmin", new Docker.ContainerArgs
     {
         Name = "pgAdmin",
         Image = pgAdminImage.Latest,
         Ports = new InputList<Docker.Inputs.ContainerPortArgs>{
          new Docker.Inputs.ContainerPortArgs{
             Internal = 80,
             External = 80
          }
       },
         Envs = new InputList<string>{
          "PGADMIN_DEFAULT_EMAIL=user@bantads.com",
          "PGADMIN_DEFAULT_PASSWORD=bantads"
       },
         NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs>{
          new Docker.Inputs.ContainerNetworksAdvancedArgs{
             Name = network.Name
          }
       }
     }, new CustomResourceOptions
     {
         DependsOn = postgressDbImage
     });

}));

static void CreateRabbitMQContainer(string rabbitMQImageName, Docker.Network network)
{
    var rabbitMQImage = StackHelper.GetDockerImage(rabbitMQImageName, "RabbitMQ");

    var rabbitMQContainer = new Docker.Container("RabbitMQContainer", new Docker.ContainerArgs
    {
        Name = "rabbit-service",
        Image = rabbitMQImage.Latest,
        Ports = new InputList<Docker.Inputs.ContainerPortArgs>{
         new Docker.Inputs.ContainerPortArgs{
            Internal = 15672,
            External = 15672
         },
         new Docker.Inputs.ContainerPortArgs{
            Internal = 5672,
            External = 5672
         }
      },
        NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs>{
         new Docker.Inputs.ContainerNetworksAdvancedArgs{
            Name = network.Name
         }
      }
    }, new CustomResourceOptions
    {
        DependsOn = rabbitMQImage
    });
}

static void CreateManagerMicroservice(string msManagerImage, Docker.RemoteImage postgresImage, Docker.Network network)
{
    var msManager = StackHelper.GetDockerImage(msManagerImage, "bantads-manager");
    StackHelper.CreatePostgresContainer("postgres-manager", postgresImage, network, "ManagerDB", 5434);

    var managerApp = new Docker.Container("MS-Manager", new Docker.ContainerArgs
    {
        Image = msManager.Latest,
        Ports = new InputList<Docker.Inputs.ContainerPortArgs> {
                  new Docker.Inputs.ContainerPortArgs
                  {
                     Internal = 5006,
                     External = 5006
                  }
            },
        Envs = new InputList<string> {
                  $"RABBIT_HOST=rabbit-service",
                  $"RABBIT_PORT=5672",
                  $"MANAGER_HOST=postgres-manager"
            },
        NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs> {
                  new Docker.Inputs.ContainerNetworksAdvancedArgs
                  {
                     Name = network.Name
                  }
            }
    });
}


static void CreateClientMicroservice(string msClientImage, Docker.RemoteImage postgresImage, Docker.Network network)
{
    var msClient = StackHelper.GetDockerImage(msClientImage, "bantads-client");
    StackHelper.CreatePostgresContainer("postgres-client", postgresImage, network, "ClientDB",5435);

    var clientApp = new Docker.Container("MS-Client", new Docker.ContainerArgs
    {
        Image = msClient.Latest,
        Ports = new InputList<Docker.Inputs.ContainerPortArgs> {
                  new Docker.Inputs.ContainerPortArgs
                  {
                     Internal = 5009,
                     External = 5009
                  }
            },
        Envs = new InputList<string> {
                  $"RABBIT_HOST=rabbit-service",
                  $"RABBIT_PORT=5672",
                  $"CLIENT_HOST=postgres-client"
            },
        NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs> {
                  new Docker.Inputs.ContainerNetworksAdvancedArgs
                  {
                     Name = network.Name
                  }
            }
    });
}

static void CreateSaga(string msSagaImage, Docker.Network network)
{
    var saga = StackHelper.GetDockerImage(msSagaImage, "bantads-saga");

    var sagaApp = new Docker.Container("Saga", new Docker.ContainerArgs
    {
        Image = saga.Latest,
        Ports = new InputList<Docker.Inputs.ContainerPortArgs> {
                  new Docker.Inputs.ContainerPortArgs
                  {
                     Internal = 5007,
                     External = 5007
                  }
            },
        Envs = new InputList<string> {
                  $"RABBIT_HOST=rabbit-service",
                  $"RABBIT_PORT=5672"
            },
        NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs> {
                  new Docker.Inputs.ContainerNetworksAdvancedArgs
                  {
                     Name = network.Name
                  }
            }
    });
}

static void CreateFrontEnd(string msFrontImagem, Docker.Network network)
{
    var front = StackHelper.GetDockerImage(msFrontImagem, "bantads-front");

    var frontApp = new Docker.Container("front", new Docker.ContainerArgs
    {
         Image = front.Latest,
         Ports = new InputList<Docker.Inputs.ContainerPortArgs> {
            new Docker.Inputs.ContainerPortArgs
            {
               Internal = 80,
               External = 4002
            }
         },
         NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs> {
            new Docker.Inputs.ContainerNetworksAdvancedArgs
            {
               Name = network.Name
            }
         }
    });
}


static void CreateApiGateway(string msGatewayImage, Docker.Network network)
{
    var gateway = StackHelper.GetDockerImage(msGatewayImage, "bantads-gateway");

    var gatewayApp = new Docker.Container("gateway", new Docker.ContainerArgs
    {
         Image = gateway.Latest,
         Ports = new InputList<Docker.Inputs.ContainerPortArgs> {
            new Docker.Inputs.ContainerPortArgs
            {
               Internal = 8080,
               External = 3000
            }
         },
         NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs> {
            new Docker.Inputs.ContainerNetworksAdvancedArgs
            {
               Name = network.Name
            }
         }
    });
}

static void CreateAuthMicroservice(string mongoDbImageName, string authImage, Docker.Network network)
{
    var msAuth = StackHelper.GetDockerImage(authImage, "bantads-auth");
    var mongoDbImage = StackHelper.GetDockerImage(mongoDbImageName, "MongoDB");

    var mongoDbContainer = new Docker.Container("MongoDBContainer", new Docker.ContainerArgs
    {
        Name = "mongo-database",
        Image = mongoDbImage.Latest,
        Ports = new InputList<Docker.Inputs.ContainerPortArgs>{
         new Docker.Inputs.ContainerPortArgs{
            Internal = 27017,
            External = 27017
         }
      },
        NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs>{
         new Docker.Inputs.ContainerNetworksAdvancedArgs{
            Name = network.Name
         }
      }
    });

    var authApp = new Docker.Container("MS-Auth", new Docker.ContainerArgs
    {
        Image = msAuth.Latest,
        Ports = new InputList<Docker.Inputs.ContainerPortArgs> {
         new Docker.Inputs.ContainerPortArgs
         {
            Internal = 5002,
            External = 5002
         }
      },
        Envs = new InputList<string> {
            $"RABBIT_HOST=rabbit-service",
            $"RABBIT_PORT=5672",
            $"MONGO_HOST=mongo-database",
            $"MONGO_PORT=27017"
      },
        NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs> {
            new Docker.Inputs.ContainerNetworksAdvancedArgs
            {
               Name = network.Name
            }
      }
    });
}


static void CreateAccountMicroservice(string imageName, Docker.RemoteImage postgresImage, Docker.Network network)
{

    StackHelper.CreatePostgresContainer("postgres-account-write", postgresImage, network, "AccountWriteDB",5436);
    StackHelper.CreatePostgresContainer("postgres-account-read", postgresImage, network, "AccountReadDB",5437);

    var msAccount = StackHelper.GetDockerImage(imageName, "bantads-account");

    var accountApp = new Docker.Container("MS-Account", new Docker.ContainerArgs
    {
        Image = msAccount.Latest,
        Ports = new InputList<Docker.Inputs.ContainerPortArgs> {
         new Docker.Inputs.ContainerPortArgs
         {
            Internal = 5005,
            External = 5005
         }
      },
        Envs = new InputList<string> {
            $"RABBIT_HOST=rabbit-service",
            $"RABBIT_PORT=5672",
            $"ACCOUNT_READ=postgres-account-read",
            $"ACCOUNT_WRITE=postgres-account-write"
      },
        NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs> {
            new Docker.Inputs.ContainerNetworksAdvancedArgs
            {
               Name = network.Name
            }
      }
    });
 
}

static void CreateNotificationMicroservice(string msNotificationImage, Docker.Network network)
{

    var msNotification = StackHelper.GetDockerImage(msNotificationImage, "bantads-notification");

    var notificationApp = new Docker.Container("MS-Notification", new Docker.ContainerArgs
    {
        Image = msNotification.Latest,
        Ports = new InputList<Docker.Inputs.ContainerPortArgs> {
         new Docker.Inputs.ContainerPortArgs
         {
            Internal = 5003,
            External = 5003
         }
      },
        Envs = new InputList<string> {
            $"RABBIT_HOST=rabbit-service",
            $"RABBIT_PORT=5672"
      },
        NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs> {
            new Docker.Inputs.ContainerNetworksAdvancedArgs
            {
               Name = network.Name
            }
      }
    });
}