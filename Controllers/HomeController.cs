// HomeController.cs
using ChatBot.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Principal;
using Microsoft.Extensions.Configuration;
using ZXing;
using ZXing.CoreCompat.System.Drawing;
using System.Drawing;
using ZXing.Common;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Hosting.Server;
using ZXing.QrCode;
using ChatBotSQL;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System;
using ChatBotSQL.Models;

public class HomeController : Controller
{
    private readonly UserDataService _userDataService;
    private readonly IConfiguration _configuration;
    private const string LastSubmitTimeKey = "LastSubmitTime";

    public HomeController(UserDataService userDataService, IConfiguration configuration)
    {
        _userDataService = userDataService;
        _configuration = configuration;
    }

    public IActionResult Index(UserData user)
    {
        PrepareDropdownOptions();

        if (user.Pitanje != null && user.Pitanje != "")
        {
            // Provjerite je li odabrana opcija iz padajućeg izbornika
            if (!string.IsNullOrEmpty(user.Pitanje) && !string.IsNullOrWhiteSpace(user.Pitanje))
            {
                ViewBag.FlashMessage = "Vaš upit je zaprimljen.";
            }
        }

        if (!String.IsNullOrEmpty(user.BrojUgovora) && !String.IsNullOrEmpty(user.Pitanje) && (user.BrojUgovora.All(char.IsDigit)) && (user.Pitanje.All(char.IsDigit)))
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            string broj_Ugovora;
            string status_Ugovora = "";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand provjera = new SqlCommand("SELECT CHATBOT_TEMP.SPOG FROM [WEB_CHAT].[DBO].[CHATBOT_TEMP] CHATBOT_TEMP WHERE CHATBOT_TEMP.SPOG = @SPOG", connection);
                provjera.Parameters.AddWithValue("@SPOG", Hashing.ToSHA256(user.BrojUgovora));
                broj_Ugovora = (string)provjera.ExecuteScalar();

                if (String.IsNullOrEmpty(broj_Ugovora))
                {
                    return Redirect("/");
                }
                if (!String.IsNullOrEmpty(broj_Ugovora))
                {
                    provjera = new SqlCommand("SELECT SPOG FROM [WEB_CHAT].[DBO].[CHATBOT_STATUS] CHATBOT_STATUS WHERE CONVERT(DATE,DATUM) = CONVERT(DATE,GETDATE()) AND SPOG = @SPOG", connection);
                    provjera.Parameters.AddWithValue("@SPOG", Hashing.ToSHA256(user.BrojUgovora));
                    object result = provjera.ExecuteScalar();
                    if (result != null) { status_Ugovora = result.ToString(); }
                    if (!String.IsNullOrEmpty(status_Ugovora))
                    {
                        return Redirect("/");
                    }

                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandText = "INSERT INTO [WEB_CHAT].[DBO].[CHATBOT_STATUS](SPOG,DATUM,STAT)   VALUES(@param1,@param2,@param3)";

                    cmd.Parameters.AddWithValue("@param1", user.BrojUgovora);
                    cmd.Parameters.AddWithValue("@param2", DateTime.Now);
                    cmd.Parameters.AddWithValue("@param3", user.Pitanje);

                    cmd.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        return View();
    }

    [HttpPost]
    public IActionResult VerifyUser(string userId, string contractNumber)
    {
        // Čitanje DelaySeconds iz konfiguracije
        int delaySeconds = _configuration.GetValue<int>("AppSettings:DelaySeconds");

        // Provjera vremenskog ograničenja
        DateTime lastSubmitTime;
        if (HttpContext.Session.TryGetValue(LastSubmitTimeKey, out var lastSubmitTimeBytes))
        {
            lastSubmitTime = DateTime.FromBinary(BitConverter.ToInt64(lastSubmitTimeBytes, 0));
            var timeSinceLastSubmit = DateTime.Now - lastSubmitTime;

            if (timeSinceLastSubmit.TotalSeconds < delaySeconds)
            {
                ViewBag.FlashMessage = $"Molimo pričekajte {delaySeconds - timeSinceLastSubmit.TotalSeconds:F0} sekundi prije ponovnog unosa.";
                return View("Index");
            }
        }

        // Pohranite trenutno vrijeme kao zadnje vrijeme slanja
        HttpContext.Session.Set(LastSubmitTimeKey, BitConverter.GetBytes(DateTime.Now.ToBinary()));

        // Provjera da su uneseni podaci ispravni
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(contractNumber) || !contractNumber.All(char.IsDigit))
        {
            TempData["FlashMessage"] = "Broj ugovora i email adresa moraju biti ispravni.";
            return RedirectToAction("Index");
        }

        // Dohvat korisnika iz baze
        var userData = _userDataService.GetUserByIdAndContract(userId, contractNumber);

        if (userData == null)
        {
            ViewBag.FlashMessage = "Korisnik nije pronađen.";
            var viewModel = new UserDataModel();
            return View("Index",viewModel);
            
        }

        // Provjerite ima li email adresu
        if (string.IsNullOrEmpty(userData.Email))
        {
            ViewBag.FlashMessage = "Korisnik nema email adresu.";
            var viewModel = new UserDataModel();
            return View("Index", viewModel);
        }

        // Generirajte verifikacijski kod i pošaljite ga na email
        string verificationCode = GenerateVerificationString();
        SendVerificationEmail(userData.Email, verificationCode);

        // Spremite verifikacijski kod u sesiju ili bazu podataka
        HttpContext.Session.SetString("VerificationCode", verificationCode);
        HttpContext.Session.SetString("UserId", userId);
        HttpContext.Session.SetString("ContractNumber", contractNumber);

        return RedirectToAction("EnterVerificationCode");
    }

    private string GenerateVerificationString()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        Random random = new Random();
        return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    //private void SendVerificationEmail(string email, string verificationCode)
    //{
    //    // Pročitaj postavke e-pošte iz konfiguracije
    //    var emailSettings = _configuration.GetSection("EmailSettings").Get<EmailSettings>();
    //    // Implementacija slanja emaila
    //    MailMessage mail = new MailMessage(emailSettings.SenderAddress, email);
    //    mail.Subject = emailSettings.Subject;
    //    mail.Body = string.Format(emailSettings.Body, verificationCode); // Koristi string.Format za umetanja verifikacijskog koda

    //    // Kreiraj SmtpClient
    //    SmtpClient client = new SmtpClient(emailSettings.SmtpServer, emailSettings.Port)
    //    {
    //        Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password),
    //        EnableSsl = emailSettings.EnableSsl
    //    };

    //    //client.Send(mail);
    //}
    private void SendVerificationEmail(string email, string verificationCode)
    {
        // Pročitaj postavke e-pošte iz konfiguracije
        var emailSettings = _configuration.GetSection("EmailSettings").Get<EmailSettings>();

        // Kreiraj email poruku
        MailMessage mail = new MailMessage(emailSettings.SenderAddress, email)
        {
            Subject = emailSettings.Subject,
            Body = string.Format(emailSettings.Body, verificationCode)
        };

        using (SmtpClient client = new SmtpClient(emailSettings.SmtpServer, emailSettings.Port))
        {
            client.EnableSsl = emailSettings.EnableSsl;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false; // uvijek isključi defaultne credse

            // Ako username nije prazan → koristi autentifikaciju
            if (!string.IsNullOrWhiteSpace(emailSettings.Username))
            {
                client.Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password);
            }
            else
            {
                client.Credentials = null; // bez AUTH
            }

            client.Send(mail);
        }
    }

    public IActionResult EnterVerificationCode()
    {
        return View();
    }

    [HttpPost]
    public IActionResult VerifyInput(string inputCode)
    {
        var storedCode = HttpContext.Session.GetString("VerificationCode");

        if (storedCode != null && storedCode.Equals(inputCode, StringComparison.OrdinalIgnoreCase))
        {
            // Ovdje možete omogućiti pristup podacima korisniku
            var userId = HttpContext.Session.GetString("UserId");
            var contractNumber = HttpContext.Session.GetString("ContractNumber");
            var userData = _userDataService.GetUserByIdAndContract(userId, contractNumber);

            PrepareDropdownOptions();
            return View("GetUserData", userData); // Prikaz podataka
        }

        //ModelState.AddModelError("", "Neispravan verifikacijski kod.");
        //return View();
        ViewBag.FlashMessage = "Neispravan verifikacijski kod.";
        PrepareDropdownOptions();
        var viewModel = new UserDataModel();
        return View("EnterVerificationCode", viewModel);
    }
    public IActionResult CreateBarcode(string id, string b1, string b2)
    {
        string connectionString = _configuration.GetConnectionString("DefaultConnection");
        string barcode;

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            //connection.Open();
            //SqlCommand provjera = new SqlCommand("SELECT BARKOD1 FROM [WEB_CHAT].[DBO].[CHATBOT_TEMP_2] CHATBOT_TEMP WHERE CHATBOT_TEMP_2.SPOG =" + id, connection);
            //SqlCommand provjera2 = new SqlCommand("SELECT BARKOD2 FROM [WEB_CHAT].[DBO].[CHATBOT_TEMP_2] CHATBOT_TEMP WHERE CHATBOT_TEMP_2.SPOG =" + id, connection);
            //barcode = (string)provjera.ExecuteScalar() +  id + (string)provjera2.ExecuteScalar();
            barcode = b1 + id + b2 + id;
            //connection.Close(); 

            if (String.IsNullOrEmpty(barcode))
            {
                return NoContent();
            }

            BarcodeWriter writer = new BarcodeWriter()
            {
                Format = BarcodeFormat.PDF_417,
                Options = new ZXing.Common.EncodingOptions { Margin = 5 }
            };

            Bitmap barcodeBitmap = writer.Write(barcode);

            var memoryStream = new MemoryStream();
            {
                barcodeBitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return File(memoryStream, "image/png");
            }
        }

    }
    private void PrepareDropdownOptions()
    {
        var dropdownOptions = _configuration.GetSection("AppSettings:DropdownOptions")
            .Get<List<DropdownOption>>();

        List<SelectListItem> selectListItems = dropdownOptions.Select(option => new SelectListItem
        {
            Value = option.Value,
            Text = option.Text
        }).ToList();

        ViewBag.DropdownOptions = selectListItems;
    }
}
public class DropdownOption
{
    public string Value { get; set; }
    public string Text { get; set; }
}
