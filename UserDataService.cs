

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Drawing;
using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ChatBotSQL;

[ValidateReCaptcha]
public class UserDataService
{
    private readonly IConfiguration _configuration;

    public UserDataService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public UserData GetUserByIdAndContract(string userId, string contractNumber)
    {
        string connectionString = _configuration.GetConnectionString("DefaultConnection");

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            SqlCommand command = new SqlCommand("SELECT sifra_korisnika, CHATBOT_TEMP.SPOG, DATUM_STANJA, DUG_UGOVOR, BROJ_OTVORENIH_RATA_UGOVOR, DUG_KLIJENT, BROJ_OTVORENIH_RATA_KLIJENT, " +
                "BROJ_NEPLACENIH_RATA_UGOVOR_TEKST,BROJ_NEPLACENIH_RATA_KLIJENT_TEKST, BROJ_PREOSTALIH_RATA, BROJ_PREOSTALIH_RATA_TEKST, BROJ_PREOSTALIH_RATA_TEKST_PRIJE, NEDOSPIJELO_UGOVOR, " +
                "NEDOSPIJELO_KLIJENT,CHATBOT_STATUS.STAT AS 'STAT', BROJ_AKTIVNIH, SKLC1, SKLC2, BARKOD1 AS 'BARKOD1', BARKOD2 AS 'BARKOD2', CHATBOT_TEMP.EMAIL " +
                "FROM [WEB_CHAT].[DBO].[CHATBOT_TEMP] CHATBOT_TEMP " +
                "LEFT OUTER JOIN [WEB_CHAT].[DBO].[CHATBOT_STATUS] CHATBOT_STATUS ON CHATBOT_TEMP.SPOG = CONVERT(VARCHAR(70) , HASHBYTES('SHA2_256',CHATBOT_STATUS.SPOG),2) AND CONVERT(DATE,CHATBOT_STATUS.DATUM) = CONVERT(DATE,GETDATE()) " +
                "WHERE CHATBOT_TEMP.EMAIL = @UserId AND CHATBOT_TEMP.SPOG = @ContractNumber", connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ContractNumber", Hashing.ToSHA256(contractNumber));

            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new UserData
                    {
                        SifraKorisnika = reader["sifra_korisnika"].ToString(),
                        BrojUgovora = reader["SPOG"].ToString(),
                        DatumStanja = (DateTime)reader["DATUM_STANJA"],
                        TrenutniDug = (Decimal)reader["DUG_UGOVOR"],
                        BrojOtvorenihRataUgovor = (int)reader["BROJ_OTVORENIH_RATA_UGOVOR"],
                        TrenutniDugKlijent = (Decimal)reader["DUG_KLIJENT"],
                        BrojOtvorenihRataKlijent = (int)reader["BROJ_OTVORENIH_RATA_KLIJENT"],
                        BrojNeplacenihRataUgovor = reader["BROJ_NEPLACENIH_RATA_UGOVOR_TEKST"].ToString(),
                        BrojNeplacenihRataKlijent = reader["BROJ_NEPLACENIH_RATA_KLIJENT_TEKST"].ToString(),
                        BrojPreostalihRata = reader["BROJ_PREOSTALIH_RATA"].ToString(),
                        BrojPreostalihRataTekst = reader["BROJ_PREOSTALIH_RATA_TEKST"].ToString(),
                        BrojPreostalihRataTekstPrije = reader["BROJ_PREOSTALIH_RATA_TEKST_PRIJE"].ToString(),
                        NedospijeloUgovor = (Decimal)reader["NEDOSPIJELO_UGOVOR"],
                        NedospijeloKlijent = (Decimal)reader["NEDOSPIJELO_KLIJENT"],
                        Stat = reader["STAT"].ToString(),
                        BrojAktivnih = (int)reader["BROJ_AKTIVNIH"],
                        Sklc1 = reader["SKLC1"].ToString(),
                        Sklc2 = reader["SKLC2"].ToString(),
                        Barcode1 = reader["BARKOD1"].ToString(),
                        Barcode2 = reader["BARKOD2"].ToString(),
                        Email = reader["Email"].ToString(), // Pretpostavljamo da imate Email u bazi
                        BrUgo = contractNumber
                        // Dodajte ostale potrebne atribute...
                    };
                }
            }
        }

        return null; // Korisnik nije pronađen
    }
}
