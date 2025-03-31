namespace System
{
    public delegate void EventHandler(object? sender, EventArgs e);

    public delegate void EventHandler<TEventArgs>(object? sender, TEventArgs e); // Removed TEventArgs constraint post-.NET 4
}