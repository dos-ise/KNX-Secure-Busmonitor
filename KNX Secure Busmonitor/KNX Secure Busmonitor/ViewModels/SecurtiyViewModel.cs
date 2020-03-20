using System;
using System.Collections.Generic;
using System.Text;

namespace Busmonitor.ViewModels
{
  using System.Linq;
  using System.Security;

  using Plugin.FilePicker;
  using Plugin.FilePicker.Abstractions;

  class SecurtiyViewModel
  {
    private string _fileName;

    private SecureString MakeStringSecure(string plain)
    {
      //Not very good handling
      SecureString sec = new SecureString();
      string pwd = plain; /* Not Secure! */
      pwd.ToCharArray().ToList().ForEach(sec.AppendChar);
      /* and now : seal the deal */
      sec.MakeReadOnly();
      return sec;
    }

    public async void OnAddKeyring()
    {
      FileData fileData = await CrossFilePicker.Current.PickFile();
      if (fileData == null)
        return; // user canceled file picking
      _fileName = fileData.FileName;
    }
  }
}
