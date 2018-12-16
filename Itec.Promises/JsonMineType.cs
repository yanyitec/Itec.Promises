using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Itec.Promises
{
    public class JsonMineType : MineType
    {
        public JsonMineType(string value=null):base(value??"json") {
            this.RequestAsync = false;
            this.ResponseAsync = false;
            this.RequestKind = MineTypeKinds.Any;
            this.ResponseKind = MineTypeKinds.Stream;
        }
        public override object Deserialize(Stream stream,Type type)
        {
            using (var reader = new System.IO.StreamReader(stream)) {
                JsonSerializer serializer = new JsonSerializer();
                return serializer.Deserialize(reader, type);
                
            }
                
        }
    }
}
