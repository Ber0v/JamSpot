namespace JamSpotApp.Models.Group
{
    public class GroupListViewModel
    {
        public IEnumerable<GroupViewModel> Groups { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool UserHasGroup { get; set; }
        public bool IsMemberOfGroup { get; set; }
    }
}
