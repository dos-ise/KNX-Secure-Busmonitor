using Knx.Falcon.Logging;

namespace Busmonitor.ViewModels
{
    public class MyLoggerFactory : IFalconLoggerFactory
    {
        public IFalconLogger GetLogger(string name)
        {
            return new FLogger();
        }
    }
}