// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Dashboard.Services;

namespace DevHome.Dashboard.ComSafeWidgetObjects;

internal sealed class ComSafeHelpers
{
    public static async Task<List<ComSafeWidgetDefinition>> GetAllOrderedComSafeWidgetDefinitions(IWidgetHostingService widgetHostingService)
    {
        var unsafeWidgetDefinitions = await widgetHostingService.GetWidgetDefinitionsAsync();
        var comSafeWidgetDefinitions = new List<ComSafeWidgetDefinition>();
        foreach (var unsafeWidgetDefinition in unsafeWidgetDefinitions)
        {
            var id = await ComSafeWidgetDefinition.GetIdFromUnsafeWidgetDefinitionAsync(unsafeWidgetDefinition);
            if (!string.IsNullOrEmpty(id))
            {
                var comSafeWidgetDefinition = new ComSafeWidgetDefinition(id);
                if (await comSafeWidgetDefinition.PopulateAsync())
                {
                    comSafeWidgetDefinitions.Add(comSafeWidgetDefinition);
                }
            }
        }

        comSafeWidgetDefinitions = comSafeWidgetDefinitions.OrderBy(def => def.DisplayTitle).ToList();
        return comSafeWidgetDefinitions;
    }
}
