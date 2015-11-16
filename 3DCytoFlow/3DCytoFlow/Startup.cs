using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(_3DCytoFlow.Startup))]
namespace _3DCytoFlow
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
