using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using GroupDocs.Viewer.WebForm.FrontEnd.BusinessLayer;

namespace GDWebAPI
{
    
    [ServiceContract]
    public interface IGDWeb
    {
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "ViewDocument", ResponseFormat = WebMessageFormat.Json)]

        ViewDocumentResponse ViewDocument(ViewDocumentParameters request);

 
    }
}
