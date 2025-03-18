using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using SignalRChartSample.Models;

namespace SignalRChartSample.Controllers
{
    public class PieChartController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHubContext<RealTimeChartHub> _hubContext;

        public PieChartController(IConfiguration configuration, IHubContext<RealTimeChartHub> hubContext)
        {
            _configuration = configuration;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Save(TeamModel requestModel)
        {
            await AddData(requestModel);

            var lst = await GetData();
            var model = new
            {
                Labels = lst.Select(x => x.Name).ToArray(),
                Series = lst.Select(x => x.Value).ToArray()
            };

            var jsonData = JsonConvert.SerializeObject(model);
            await _hubContext.Clients.All.SendAsync("ClientReceiveMessage", jsonData);
            return RedirectToAction("Create");
        }

        // Read
        private async Task<List<TeamModel>> GetData()
        {
            using (IDbConnection db = new SqlConnection(_configuration.GetConnectionString("DbConnection")))
            {
                string query = "SELECT * FROM Tbl_Team";
                var result = await db.QueryAsync<TeamModel>(query);
                return result.ToList();
            }
        }

        // Insert
        private async Task AddData(TeamModel teamModel)
        {
            teamModel.Id = Guid.NewGuid().ToString();
            using (IDbConnection db = new SqlConnection(_configuration.GetConnectionString("DbConnection")))
            {
                string query = "INSERT INTO Tbl_Team (Id, Name, Value) VALUES (@Id, @Name, @Value)";
                await db.ExecuteAsync(query, teamModel);
            }
        }
    }
}
