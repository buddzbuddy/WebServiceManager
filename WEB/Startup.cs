using Microsoft.Owin;
using Newtonsoft.Json;
using Owin;
using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;

[assembly: OwinStartupAttribute(typeof(WEB.Startup))]
namespace WEB
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
