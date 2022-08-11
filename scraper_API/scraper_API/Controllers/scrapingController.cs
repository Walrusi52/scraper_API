using scraper_API.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace scraper_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class scrapingController : ControllerBase
    {
        static List<Models.Results> results = new List<Models.Results>();
        static SearchParameters paras = new SearchParameters();

        [Route("ucc")]
        [HttpGet]
        public List<Models.Results> GetResults()
        {
            return results;
        }

        public async Task Scrape(string p_name, string p_last, string p_first, string p_middle, string p_suffix, string p_city, string p_state)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("https://appext20.dos.ny.gov/pls/ucc_public/web_inhouse_search.print_ucc1_list");
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("p_name", p_name),
                    new KeyValuePair<string, string>("p_last", p_last),
                    new KeyValuePair<string, string>("p_first", p_first),
                    new KeyValuePair<string, string>("p_middle", p_middle),
                    new KeyValuePair<string, string>("p_suffix", p_suffix),
                    new KeyValuePair<string, string>("p_city", p_city),
                    new KeyValuePair<string, string>("p_state", p_state),
                    new KeyValuePair<string, string>("p_lapsed", "1"),
                    new KeyValuePair<string, string>("p_filetype", "ALL")
                });

                var response = await httpClient.PostAsync("", content);

                string result = response.Content.ReadAsStringAsync().Result;

                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(result);
                List<HtmlNode> nodes = htmlDocument.DocumentNode.Descendants().ToList();

                int dname = 0;
                bool party = false;
                int fileNo = 0;
                string file_no_temp = "";
                bool first = true;
                List<Models.File> file = new List<Models.File>();

                string Debtor_Names = "";
                string Secured_Party_Names = "";
                string File_No = "";
                string File_Date = "";
                string Refile_Date = "";
                string Filing_Type = "";
                string Pages = "";
                string Images = "";

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].Name == "#text" && (nodes[i].ParentNode.Name.ToUpper() == "FONT" || nodes[i].ParentNode.Name.ToUpper() == "B"))
                    {
                        if (nodes[i].InnerHtml == "Debtor Names:")
                        {
                            if (!first)
                            {
                                results.Add(new Models.Results { Debtor_Names = Debtor_Names, Secured_Party_Names = Secured_Party_Names, Files = file });
                                file = new List<Models.File>();
                            }
                            else first = false;
                            fileNo = 0;
                            dname++;
                        }
                        else if (dname > 0)
                        {
                            if (dname == 1)
                            {
                                Debtor_Names = nodes[i].InnerHtml;
                                dname++;
                            }
                            else
                            {
                                Debtor_Names += " " + nodes[i].InnerHtml;
                                dname = 0;
                            }
                        }
                        else if (nodes[i].InnerHtml == "Secured Party Names:")
                        {
                            party = true;
                        }
                        else if (party)
                        {
                            Secured_Party_Names = nodes[i].InnerHtml;
                            party = false;
                        }
                        else if (nodes[i].InnerHtml == "Image")
                        {
                            fileNo++;
                        }
                        else if (fileNo > 0)
                        {
                            switch (fileNo)
                            {
                                case 1:
                                    if (nodes[i].InnerHtml == "* Images marked NA are not available on this webpage.")
                                    {
                                        fileNo = 0;
                                        break;
                                    }
                                    file_no_temp = nodes[i].InnerHtml;
                                    fileNo++;
                                    break;
                                case 2:
                                    File_No = file_no_temp;
                                    File_Date = nodes[i].InnerHtml;
                                    fileNo++;
                                    break;
                                case 3:
                                    Refile_Date = nodes[i].InnerHtml;
                                    fileNo++;
                                    break;
                                case 4:
                                    Filing_Type = nodes[i].InnerHtml;
                                    fileNo++;
                                    break;
                                case 5:
                                    Pages = nodes[i].InnerHtml;
                                    fileNo++;
                                    break;
                                case 6:
                                    if (nodes[i].NextSibling is not null)
                                    {
                                        if (nodes[i].NextSibling.Name.ToUpper() == "A")
                                        {
                                            Images = nodes[i + 1].Attributes[0].Value;
                                        }
                                        else Images = nodes[i].InnerHtml;
                                    }
                                    else Images = nodes[i].InnerHtml;
                                    fileNo = 1;
                                    file.Add(new Models.File { File_No = File_No, File_Date = File_Date, Refile_Date = Refile_Date, Filing_Type = Filing_Type, Pages = Pages, Images = Images });
                                    break;
                                default:
                                    fileNo = 0;
                                    break;
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(Debtor_Names))
                {
                    results.Add(new Models.Results { Debtor_Names = Debtor_Names, Secured_Party_Names = Secured_Party_Names, Files = file });
                }
            }
        }

        [Route("parameters")]
        [HttpPost]
        public SearchParameters AddParamerters(SearchParameters para)
        {
            paras = para;
            if (string.IsNullOrWhiteSpace(paras.Business_Name) && string.IsNullOrWhiteSpace(paras.Last_Name))
            {
                throw new Exception("Business Name or Last Name must be provided");
            }
            results = new List<Models.Results>();
            _ = Scrape(paras.Business_Name, paras.Last_Name, paras.First_Name, paras.Middle_Name, paras.Suffix, paras.City, paras.State);
            return para;
        }
    }
}
