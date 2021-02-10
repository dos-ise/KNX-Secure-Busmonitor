using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Xamarin.CommunityToolkit.Behaviors;

namespace Busmonitor.Behavior
{
  public class IpAddressValidationBehavior : TextValidationBehavior
  {
    protected override bool Validate(object value) => base.Validate(value) && IPAddress.TryParse(value?.ToString(), out var ip);
  }
}
