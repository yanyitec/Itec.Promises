using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;

namespace Itec.Promises
{
    public class UrlMineType :MineType
    {
        static ConcurrentDictionary<string, Func<object, string>> Serializers = new ConcurrentDictionary<string, Func<object, string>>();
        public UrlMineType(string value=null):base(value??"application/x-www-form-urlencoded") {
            
        }

        public override string Serialize(object data)
        {
            if (data == null) return null;
            var objType = data.GetType();
            if (objType == typeof(string)) return data.ToString();
            if (objType.IsClass) {
                var serializer = Serializers.GetOrAdd(objType.FullName,(n)=> MakeSerialize(objType));
                return serializer(data);
            }
            return data.ToString();
        }

        static MethodInfo ToStringMethodInfo = typeof(object).GetMethod("ToString");
        static MethodInfo UrlEncodeMethodInfo = typeof(HttpUtility).GetMethod("UrlEncode");
        static MethodInfo GetNameMethodInfo = typeof(Enum).GetMethod("GetName");
        static MethodInfo AppendMethodInfo = typeof(StringBuilder).GetMethod("Append",new Type[] { typeof(string)});
        #region serialize
        static Func<object, string> MakeSerialize(Type objType) {
            ParameterExpression objParamExpr = Expression.Parameter(typeof(object),"objParam");
            var block = MakeSerialize(objType,objParamExpr);
            var lamda= Expression.Lambda<Func<object, string>>(block, objParamExpr);
            return lamda.Compile();
        }
        static Expression MakeSerialize(Type objType,ParameterExpression objParamExpr) {
            
            var locals = new List<ParameterExpression>();
            var codes = new List<Expression>();

            var objExpr = Expression.Parameter(objType, "obj");
            locals.Add(objExpr);
            codes.Add(Expression.Assign(objExpr, Expression.Convert(objParamExpr,objType)));

            var sbExpr = Expression.Parameter(typeof(StringBuilder),"sb");
            locals.Add(sbExpr);
            codes.Add(Expression.Assign(sbExpr,Expression.New(typeof(StringBuilder))));
            MakeSerialize(locals, codes, objExpr, sbExpr);

            var retLabel = Expression.Label();
            var retExpr = Expression.Return(retLabel, Expression.Call(sbExpr, ToStringMethodInfo));
            codes.Add(retExpr);
            codes.Add(Expression.Label(retLabel,Expression.Constant(null,typeof(string))));
            return Expression.Block(locals, codes);
        }
        static void MakeSerialize(List<ParameterExpression> locals, List<Expression> codes,
            ParameterExpression objExpr,ParameterExpression sbExpr) {
            
            
            var props = objExpr.Type.GetProperties();
            foreach (var prop in props) AppendExpr(locals, codes, sbExpr, objExpr, prop);
            var fields = objExpr.Type.GetFields();
            foreach (var prop in fields) AppendExpr(locals, codes, sbExpr, objExpr, prop);
            
        }
        static void AppendExpr(List<ParameterExpression> locals, List<Expression> codes
            ,Expression sbExpr, Expression objExpr, MemberInfo info) {
            var propType = GetPropertyType(info);
            var txtExpr = GetValueTextExpr(objExpr,propType,info);
            var localExpr = Expression.Parameter(typeof(string),info.Name);
            locals.Add(localExpr);
            codes.Add(Expression.Assign(localExpr,GetValueTextExpr(objExpr,propType,info)));

            Expression addNameExpr = Expression.Call(sbExpr,AppendMethodInfo,Expression.Constant(HttpUtility.UrlEncode(info.Name)));
            var addEqualExpr = Expression.Call(addNameExpr, AppendMethodInfo, Expression.Constant("="));
            var addValueExpr = Expression.Call(addEqualExpr,AppendMethodInfo,Expression.Call(UrlEncodeMethodInfo,localExpr));
            var ckExpr = Expression.IfThen(Expression.Equal(localExpr,Expression.Constant(null,typeof(string))), addValueExpr);
        }
        static Expression GetValueTextExpr(Expression obj,Type propType, MemberInfo info) {
            Expression valueExpr = Expression.PropertyOrField(obj,info.Name);
            if (propType == typeof(string)) return valueExpr;
            var isNullable = propType.FullName.StartsWith("System.Nullable`1");
            Type actualType = propType;
            Expression nullCheckExpr = null;
            if (isNullable)
            {
                
                actualType = propType.GetGenericArguments()[0];
                nullCheckExpr = Expression.PropertyOrField(valueExpr,"HasValue");
                valueExpr = Expression.PropertyOrField(valueExpr,"Value");
            }
            if (actualType.IsEnum)
            {
                valueExpr = Expression.Call(null, GetNameMethodInfo, Expression.Constant(propType), valueExpr);
            }
            else if (actualType.IsClass)
            {
                nullCheckExpr = Expression.Equal(Expression.Constant(null, actualType), valueExpr);
                valueExpr = Expression.Call(valueExpr, ToStringMethodInfo);
            }
            else {
                valueExpr = Expression.Call(valueExpr, ToStringMethodInfo);
            }
            if (nullCheckExpr == null) return valueExpr;
            return Expression.IfThenElse(nullCheckExpr,Expression.Constant(null,typeof(string)),valueExpr);
        }

        static Type GetPropertyType(MemberInfo member) {
            if (member.MemberType == MemberTypes.Property)
            {
                var propInfo = member as PropertyInfo;
                if (!propInfo.CanRead || propInfo.GetMethod == null) return null;
                return propInfo.PropertyType;

            }
            if (member.MemberType == MemberTypes.Field)
            {
                var propInfo = member as FieldInfo;
                return propInfo.FieldType;

            }
            return null;
        }
    }
    #endregion
    #region deserialize

    #endregion
}
