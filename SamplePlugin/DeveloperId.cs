using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;

namespace SamplePlugin;
public class DeveloperId : IDeveloperId
{
    private readonly string _loginId;
    private readonly string _url;

    public DeveloperId(string loginId, string url)
    {
        _loginId = loginId;
        _url = url;
    }

    public string LoginId() => _loginId;

    public string Url() => _url;
}
