using System;
using Pulumi;
using Docker = Pulumi.Docker;
public class StackHelper
{
    public static Docker.RemoteImage GetDockerImage(string imageName, string name)
    {
        var dockerImage = new Docker.RemoteImage(name, new Docker.RemoteImageArgs
        {
            KeepLocally = true,
            Name = imageName
        });

        return dockerImage;
    }


    public static void CreatePostgresContainer(string containerName, Docker.RemoteImage image, Docker.Network network, string name, int port)
    {
        var databaseContainer = new Docker.Container(name, new Docker.ContainerArgs
        {
            Name = containerName,
            Image = image.Latest,
            Ports = new InputList<Docker.Inputs.ContainerPortArgs>{
				new Docker.Inputs.ContainerPortArgs{
					Internal = 5432,
					External = port
				}
        	},
        	Envs = new InputList<string>{
				"POSTGRES_USER=bantads",
				"POSTGRES_PASSWORD=bantads"
       		},
            NetworksAdvanced = new InputList<Docker.Inputs.ContainerNetworksAdvancedArgs>{
				new Docker.Inputs.ContainerNetworksAdvancedArgs{
					Name = network.Name
				}
      		}
    	});
    }
}
