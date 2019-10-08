using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace ThaiNationalIDCard
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IJSONRestService" in both code and config file together.
    [ServiceContract]
    public interface IJSONRestService
    {
        [OperationContract]
        void DoWork();
    }
}
