namespace WeaponsAndWizardry.Models
{
    public interface IModel
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "_etag")]
        public string ETag { get; }
    }
}
