using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchLib
{
    internal static class Converter
    {
        public static string convertToThreeLetterCode(string two)
        {
            if (mCountryCodesMapping.ContainsKey(two))
            {
                return mCountryCodesMapping[two];
            }

            return "aaa";
        }

        public static Dictionary<string, string> mCountryCodesMapping = new Dictionary<string, string>() {
           { "AF","AFG" },    // Afghanistan
           { "AL","ALB" },    // Albania
           { "AE","ARE" },    // U.A.E.
           { "AR","ARG" },    // Argentina
           { "AM","ARM" },    // Armenia
           { "AU","AUS" },    // Australia
           { "AT","AUT" },    // Austria
           { "AZ","AZE" },    // Azerbaijan
           { "BE","BEL" },    // Belgium
           { "BD","BGD" },    // Bangladesh
           { "BG","BGR" },    // Bulgaria
           { "BH","BHR" },    // Bahrain
           { "BA","BIH" },    // Bosnia and Herzegovina
           { "BY","BLR" },    // Belarus
           { "BZ","BLZ" },    // Belize
           { "BO","BOL" },    // Bolivia
           { "BR","BRA" },    // Brazil
           { "BN","BRN" },    // Brunei Darussalam
           { "CA","CAN" },    // Canada
           { "CH","CHE" },    // Switzerland
           { "CL","CHL" },    // Chile
           { "CN","CHN" },    // People's Republic of China
           { "CO","COL" },    // Colombia
           { "CR","CRI" },    // Costa Rica
           { "CZ","CZE" },    // Czech Republic
           { "DE","DEU" },    // Germany
           { "DK","DNK" },    // Denmark
           { "DO","DOM" },    // Dominican Republic
           { "DZ","DZA" },    // Algeria
           { "EC","ECU" },    // Ecuador
           { "EG","EGY" },    // Egypt
           { "ES","ESP" },    // Spain
           { "EE","EST" },    // Estonia
           { "ET","ETH" },    // Ethiopia
           { "FI","FIN" },    // Finland
           { "FR","FRA" },    // France
           { "FO","FRO" },    // Faroe Islands
           { "GB","GBR" },    // United Kingdom
           { "GE","GEO" },    // Georgia
           { "GR","GRC" },    // Greece
           { "GL","GRL" },    // Greenland
           { "GT","GTM" },    // Guatemala
           { "HK","HKG" },    // Hong Kong S.A.R.
           { "HN","HND" },    // Honduras
           { "HR","HRV" },    // Croatia
           { "HU","HUN" },    // Hungary
           { "ID","IDN" },    // Indonesia
           { "IN","IND" },    // India
           { "IE","IRL" },    // Ireland
           { "IR","IRN" },    // Iran
           { "IQ","IRQ" },    // Iraq
           { "IS","ISL" },    // Iceland
           { "IL","ISR" },    // Israel
           { "IT","ITA" },    // Italy
           { "JM","JAM" },    // Jamaica
           { "JO","JOR" },    // Jordan
           { "JP","JPN" },    // Japan
           { "KZ","KAZ" },    // Kazakhstan
           { "KE","KEN" },    // Kenya
           { "KG","KGZ" },    // Kyrgyzstan
           { "KH","KHM" },    // Cambodia
           { "KR","KOR" },    // Korea
           { "KW","KWT" },    // Kuwait
           { "LA","LAO" },    // Lao P.D.R.
           { "LB","LBN" },    // Lebanon
           { "LY","LBY" },    // Libya
           { "LI","LIE" },    // Liechtenstein
           { "LK","LKA" },    // Sri Lanka
           { "LT","LTU" },    // Lithuania
           { "LU","LUX" },    // Luxembourg
           { "LV","LVA" },    // Latvia
           { "MO","MAC" },    // Macao S.A.R.
           { "MA","MAR" },    // Morocco
           { "MC","MCO" },    // Principality of Monaco
           { "MV","MDV" },    // Maldives
           { "MX","MEX" },    // Mexico
           { "MK","MKD" },    // Macedonia (FYROM)
           { "MT","MLT" },    // Malta
           { "ME","MNE" },    // Montenegro
           { "MN","MNG" },    // Mongolia
           { "MY","MYS" },    // Malaysia
           { "NG","NGA" },    // Nigeria
           { "NI","NIC" },    // Nicaragua
           { "NL","NLD" },    // Netherlands
           { "NO","NOR" },    // Norway
           { "NP","NPL" },    // Nepal
           { "NZ","NZL" },    // New Zealand
           { "OM","OMN" },    // Oman
           { "PK","PAK" },    // Islamic Republic of Pakistan
           { "PA","PAN" },    // Panama
           { "PE","PER" },    // Peru
           { "PH","PHL" },    // Republic of the Philippines
           { "PL","POL" },    // Poland
           { "PR","PRI" },    // Puerto Rico
           { "PT","PRT" },    // Portugal
           { "PY","PRY" },    // Paraguay
           { "QA","QAT" },    // Qatar
           { "RO","ROU" },    // Romania
           { "RU","RUS" },    // Russia
           { "RW","RWA" },    // Rwanda
           { "SA","SAU" },    // Saudi Arabia
           { "CS","SCG" },    // Serbia and Montenegro (Former)
           { "SN","SEN" },    // Senegal
           { "SG","SGP" },    // Singapore
           { "SV","SLV" },    // El Salvador
           { "RS","SRB" },    // Serbia
           { "SK","SVK" },    // Slovakia
           { "SI","SVN" },    // Slovenia
           { "SE","SWE" },    // Sweden
           { "SY","SYR" },    // Syria
           { "TJ","TAJ" },    // Tajikistan
           { "TH","THA" },    // Thailand
           { "TM","TKM" },    // Turkmenistan
           { "TT","TTO" },    // Trinidad and Tobago
           { "TN","TUN" },    // Tunisia
           { "TR","TUR" },    // Turkey
           { "TW","TWN" },    // Taiwan
           { "UA","UKR" },    // Ukraine
           { "UY","URY" },    // Uruguay
           { "US","USA" },    // United States
           { "UZ","UZB" },    // Uzbekistan
           { "VE","VEN" },    // Bolivarian Republic of Venezuela
           { "VN","VNM" },    // Vietnam
           { "YE","YEM" },    // Yemen
           { "ZA","ZAF" },    // South Africa
           { "ZW","ZWE" },    // Zimbabwe
        };
    }
}
