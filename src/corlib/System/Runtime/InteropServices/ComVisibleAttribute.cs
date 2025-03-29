namespace System.Runtime.InteropServices
{
    public class ComVisibleAttribute : Attribute
    {
        private bool m_visible;

        public bool Visible
        {
            get { return m_visible; }
        }

        public ComVisibleAttribute(bool visible)
        {
            m_visible = visible;
        }
    }
}