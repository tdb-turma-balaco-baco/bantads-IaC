using System.Collections.Generic;
using Pulumi;
using Docker = Pulumi.Docker;

return await Deployment.RunAsync(() =>
{
   var configs = new Config("bantads");

   var rabbitMQImageName = configs.Require("rabbitMQ-image");
   var mongoDbImageName = configs.Require("mongoDb-image");

   //var msAccountImageName = configs.Require("account-image");
   //var dbAccountReadImageName = configs.Require("account-db-write");
   //var dbAccountWriteImageName = configs.Require("account-db-read");

   var network = new Docker.Network("network-bantads");

   //Criar imagem do MS
/*    var msAccountImage = new Docker.Image("MS-Account", new Docker.ImageArgs {  
      Build = new Docker.DockerBuild {
         Context = "../account"
      },
      ImageName = msAccountImageName,
      SkipPush = true
   }); */

   //Criar imagem do rabbitmq
   var rabbitMQImage = new Docker.RemoteImage("RabbitMQ", new Docker.RemoteImageArgs{
      KeepLocally = true,
      Name = rabbitMQImageName
   });

   var rabbitMQContainer = new Docker.Container("RabbitMQContainer", new Docker.ContainerArgs{
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
   }, new CustomResourceOptions{
      DependsOn = rabbitMQImage
   });


   //CREATING MONGO DB
   var mongoVolume = new Docker.Volume("mongo-volume");

   var mongoDbImage = new Docker.RemoteImage("MongoDB", new Docker.RemoteImageArgs{
      KeepLocally = true,
      Name = mongoDbImageName
   });

   var mongoDbContainer = new Docker.Container("MongoDBContainer", new Docker.ContainerArgs{
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
      },
      Volumes = new InputList<Docker.Inputs.ContainerVolumeArgs>{
         new Docker.Inputs.ContainerVolumeArgs{
            VolumeName = mongoVolume.Name,
            ContainerPath = "/data/db"
         }
      }
   }, new CustomResourceOptions{
      DependsOn = mongoDbImage
   });


   //Criar imagem do Banco read

   //Criar imagem do Banco write
});
