{
  "version": "1.0.2",
  "title": "LinqInfer.Owin",
  "description": "A lightweight web API framework and OWIN integration for LinqInfer",
  "language": "en-GB",
  "authors": [ "R Eyres" ],
  "packOptions": {
    "projectUrl": "https://github.com/roberino/linqinfer",
    "tags": [ "linq", "linqinfer", "owin", "rest", "webapi" ],
    "licenseUrl": "https://raw.githubusercontent.com/roberino/linqinfer/master/LICENSE",
    "repository": {
      "type": "git",
      "url": "git://github.com/roberino/linqinfer"
    }
  },
  "runtimes": {
    "win": {}
  },
  "frameworks": {
    //"netstandard1.6": {
    //  "imports": "dnxcore50",
    //  "dependencies": {
    //    /* See http://packagesearch.azurewebsites.net/ */
    //    "NETStandard.Library": "1.6.0",
    //  Replace with asp.net vnext
    //    "Owin":  "1.0",
    //    "Newtonsoft.Json": "9.0.1",
    //    "Microsoft.Owin": "3.0.1",
    //    "Microsoft.Owin.Diagnostics": "3.0.1",
    //    "Microsoft.Owin.Host.HttpListener": "3.0.1",
    //    "Microsoft.Owin.Host.SystemWeb": "3.0.1",
    //    "Microsoft.Owin.Hosting": "3.0.1",
    //    "Microsoft.Owin.SelfHost": "3.0.1"
    //  }
    //},
    "net45": {
      "frameworkAssemblies": {
        "System.Xml": "",
        "System.Xml.Linq": ""
      },
      "dependencies": {
        "System.Net.Http": "4.0.0"
      }
    },
    "net461": {
      "frameworkAssemblies": {
        "System.Xml": "",
        "System.Xml.Linq": ""
      },
      "dependencies": {
        "System.Net.Http": "4.0.0"
      }
    }
  },
  "scripts": {
    "postcompile": [
      "dotnet pack --no-build --configuration %compile:Configuration% --output ..\\..\\artifacts"
    ]
  }
}