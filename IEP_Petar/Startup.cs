using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(IEP_Petar.Startup))]
namespace IEP_Petar
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
