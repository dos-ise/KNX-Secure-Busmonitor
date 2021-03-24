using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Behaviors;

namespace Busmonitor.Behavior
{
  public class IpAddressValidationBehavior : TextValidationBehavior
  {
		protected override ValueTask<bool> ValidateAsync(object value, CancellationToken token)
    {
      return new ValueTask<bool>(IPAddress.TryParse(value?.ToString() ?? string.Empty, out var ip));
    }
  }
}
