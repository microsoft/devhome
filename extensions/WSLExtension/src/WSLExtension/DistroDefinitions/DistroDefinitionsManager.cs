// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using WSLExtension.Models;
using YamlDotNet.Serialization;

namespace WSLExtension.DistroDefinitions;

public class DistroDefinitionsManager
{
    public static async Task<List<Distro>> ReadDistroDefinitions()
    {
        var deserializer = BuildDeserializer();
        try
        {
            var uri = new Uri("ms-appx:///WSLExtension/DistroDefinitions/Definitions/distros.yaml");
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var yamlString = await FileIO.ReadTextAsync(storageFile);
            var definitions = deserializer.Deserialize<List<Distro>>(yamlString);
            return definitions;
        }
        catch
        {
            return new List<Distro>();
        }
    }

    public static IDeserializer BuildDeserializer()
    {
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer;
    }

    public static ISerializer BuildSerializer()
    {
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();
        return serializer;
    }

    public static List<Distro> Merge(List<Distro> definitions, List<Distro> registered)
    {
        return registered.Select(r =>
        {
            var definition = definitions.FirstOrDefault(d => d.Registration == r.Registration);
            if (definition == null)
            {
                r.Logo = "linux.png";
                return r;
            }

            r.Name = definition.Name;
            r.Logo = definition.Logo;
            r.WtProfileGuid = definition.WtProfileGuid;
            return r;
        }).ToList();
    }
}
