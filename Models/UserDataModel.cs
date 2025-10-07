using System.ComponentModel.DataAnnotations;

namespace ChatBot.Models
{
    public class UserDataModel
    {
        [Required(ErrorMessage = "Email adresa je obavezna.")]
        [EmailAddress(ErrorMessage = "Neispravan format email adrese.")]
        public string UserId { get; set; }
        public string ContractNumber { get; set; }

        //public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
