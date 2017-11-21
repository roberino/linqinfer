namespace LinqInfer.Data.Remoting
{
    public static class MessageExtensions
    {
        public static Message<T> AsMessage<T>(this T body, object id = null)
        {
            return new Message<T>(body, id?.ToString());
        }
    }
}