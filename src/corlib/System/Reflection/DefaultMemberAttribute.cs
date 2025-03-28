namespace System.Reflection
{
    public sealed class DefaultMemberAttribute : Attribute
    {
        // You must provide the name of the member, this is required
        public DefaultMemberAttribute(string memberName)
        {
            MemberName = memberName;
        }

        // A get accessor to return the name from the attribute.
        // NOTE: There is no setter because the name must be provided
        //    to the constructor.  The name is not optional.
        public string MemberName { get; }
    }
}
