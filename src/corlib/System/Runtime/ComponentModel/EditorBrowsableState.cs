namespace System.ComponentModel
{
    //
    // Resumen:
    //     Specifies the browsable state of a property or method from within an editor.
    public enum EditorBrowsableState
    {
        //
        // Resumen:
        //     The property or method is always browsable from within an editor.
        Always = 0,
        //
        // Resumen:
        //     The property or method is never browsable from within an editor.
        Never = 1,
        //
        // Resumen:
        //     The property or method is a feature that only advanced users should see. An editor
        //     can either show or hide such properties.
        Advanced = 2
    }
}
