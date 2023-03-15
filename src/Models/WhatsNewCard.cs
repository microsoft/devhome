using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Models;
public class WhatsNewCard
{
    public int Priority
    {
        get; set;
    }

    public string? Title
    {
        get; set;
    }

    public string? Description
    {
        get; set;
    }

    public string? LearnMoreUrl
    {
        get; set;
    }

    public string? Icon
    {
        get; set;
    }
}
