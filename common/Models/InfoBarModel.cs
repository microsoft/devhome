using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.Models;
public class InfoBarModel
{
    public string? Title
    {
        get; set;
    }

    public string? Description
    {
        get; set;
    }

    public InfoBarSeverity? Severity
    {
        get; set;
    }

    public bool? IsOpen
    {
        get; set;
    }
}

public enum InfoBarSeverity
{
    Information,
    Warning,
    Error,
}
