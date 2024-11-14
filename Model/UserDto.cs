namespace VideoProjektAspApi.Model
{
    public class UserDto
    {

        public virtual Image? Avatar { get; set; }
        public int Followers { get; set; }
        public string UserName { get; set; }
    }
}
