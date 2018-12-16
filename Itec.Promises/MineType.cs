using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Itec.Promises
{
    public class MineType
    {
        public static Dictionary<string, MineType> MineTypes = new Dictionary<string, MineType>() {
            { "json",new JsonMineType("json")}
            ,{ "url",new UrlMineType("Url")}
            ,{ "text",new MineType("text")}
        };
        public MineType(string value=null) {
            this.Value = "*";
        }
        public readonly static MineType Request = new MineType("text");
        public readonly static MineType Response = new MineType("text");
        public string Value { get; private set; }
        public MineTypeKinds RequestKind { get; protected set; }
        public MineTypeKinds ResponseKind { get; protected set; }
        public bool RequestAsync { get; protected set; }
        public bool ResponseAsync { get; protected set; }
        public virtual string Serialize(object data) {
            return data == null ? string.Empty : data.ToString();
        }

        public virtual object Deserialize(string content,Type type) {
            return null;
        }

        public virtual object Deserialize(Stream stream,Type type) { return null; }

        public virtual Task<object> DeserializeAsync(Stream stream) { return null; }
    }
}
