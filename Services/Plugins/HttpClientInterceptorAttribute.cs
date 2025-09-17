namespace qqbot.Services.Plugins;
public enum HttpClientTarget
{
    NapCatApi,     // 专门用于 NapCat API 的客户端
    ImageCache     // 专门用于图片缓存的客户端
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class HttpClientInterceptorAttribute : Attribute
{
    public HttpClientTarget Target { get; }
    public HttpClientInterceptorAttribute(HttpClientTarget target)
    {
        Target = target;
    }
}
