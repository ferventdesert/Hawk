namespace Hawk.Base.Plugins.Transformers
{
    [XFrmWork("AddNewTF","AddNew_desc","list")]
    public class AddNewTF : TransformerBase
    {
        public AddNewTF()
        {
            NewValue = "";
            NewColumn = "NewColumn";
        }

        [Browsable(false)]
        public override string KeyConfig => NewValue; 
        [LocalizedDisplayName("key_469")]
        public string NewValue { get; set; }

        public override object TransformData(IFreeDocument free)
        {
            return free.Query(NewValue);
        }
    }


}