using OpenVectorFormat.ILTFileReader;


namespace IltCliWriterAdapter.CLI
{
    public class UserData : IUserData
    {
        public UserData(byte[] data, long len, string uid)
        {
            UID = uid;
            Data = data;
            Len = len;
        }
        public byte[] Data { get; set; }

        public long Len { get; set; }

        public string UID { get; set; }
    }
}
