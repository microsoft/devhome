using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Models;
public class WhatsNewCard
{
    public WhatsNewCard(string title, string description, string? learnMoreUrl)
    {
        Title = title;
        Description = description;
        LearnMoreUrl = learnMoreUrl;
    }

    public string Title
    {
        get; private set;
    }

    public string Description
    {
        get; private set;
    }

    public string? LearnMoreUrl
    {
        get; private set;
    }
}
