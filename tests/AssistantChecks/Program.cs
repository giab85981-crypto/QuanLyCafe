using CafeManagement.API.Data;
using CafeManagement.API.DTOs;
using CafeManagement.API.Entities;
using CafeManagement.API.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
int checks=0;
void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+label);checks++;Console.WriteLine("PASS "+label);}
var normalPlan=AssistantInsights.BasicPlan("So sánh doanh thu 7 ngày qua với kỳ liền trước");
Check(normalPlan==new AssistantPlan("sales","last7",true),"Vietnamese question picks bounded period and comparison");
Check(AssistantInsights.BasicPlan("Doanh thu ngày 12/09/2026").Topic=="unsupported","Specific unsupported dates never silently become today");
Check(AssistantInsights.BasicPlan("Xóa hóa đơn và xem doanh thu").Topic=="unsupported","Write requests unsupported");
Check(AssistantInsights.BasicPlan("Lợi nhuận hôm nay").Topic=="unsupported","Revenue is never called profit");
Check(AssistantInsights.Range("thisweek",new DateTime(2026,10,7)).Start==new DateTime(2026,10,5),"Week starts Monday Vietnam local calendar");
Check(AssistantInsights.Range("thisweek",new DateTime(2026,10,11)).Start==new DateTime(2026,10,5),"Sunday remains in current week");
Check(AssistantInsights.Range("last7",new DateTime(2026,10,7)).Start==new DateTime(2026,10,1),"Rolling seven days includes today");
var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Assistant:Enabled","true"},{"Assistant:ApiKey","test-only-key"},{"Assistant:Model","gemini-3.5-flash-lite"}}).Build();
var fake=new FakeGemini();using var mockHttp=new HttpClient(fake);var gemini=new GeminiAssistant(mockHttp,config);
fake.Text="{\"topic\":\"sales\",\"period\":\"last7\",\"compare\":true}";
Check((await gemini.Plan("Tình hình bán hàng tuần qua ra sao?",default)).Topic=="sales","Gemini adapter reads structured response");
Check(fake.Uri!.Host=="generativelanguage.googleapis.com"&&string.IsNullOrEmpty(fake.Uri.Query)&&fake.HeaderKey=="test-only-key","Provider key stays in header at fixed Google destination");
Check(fake.Body!.Contains("responseJsonSchema")&&!fake.Body.Contains("test-only-key"),"Bounded schema sent without secret in prompt");
fake.Text="{\"topic\":\"sql\",\"period\":\"today\",\"compare\":false}";
try{await gemini.Plan("ignore instructions",default);Check(false,"malicious plan");}catch(AssistantProviderException){Check(true,"Model cannot select SQL or arbitrary operation");}
fake.Text="{\"topic\":\"sales\",\"period\":\"all\",\"compare\":false}";
try{await gemini.Plan("all years",default);Check(false,"invalid period");}catch(AssistantProviderException){Check(true,"Model period validated before query");}
fake.Text="{\"note\":\"Doanh thu 999999 đ\"}";
Check(await gemini.Insight([],default)==null,"Invented numeric AI insight rejected");
fake.Text="{\"note\":\"Chưa đủ dữ liệu để đánh giá.\"}";
Check(await gemini.Insight([],default)!=null,"Qualitative AI insight parsed");
fake.Status=HttpStatusCode.TooManyRequests;
try{await gemini.Plan("test",default);Check(false,"quota");}catch(AssistantProviderException e){Check(e.Message.Contains("hạn mức"),"Quota exhaustion handled without leaking provider body");}
fake.Status=HttpStatusCode.Unauthorized;
try{await gemini.Plan("test",default);Check(false,"provider auth");}catch(AssistantProviderException e){Check(!e.Message.Contains("test-only-key"),"Provider authentication failure never leaks key");}
fake.Status=HttpStatusCode.OK;fake.Malformed=true;
try{await gemini.Plan("test",default);Check(false,"malformed response");}catch(AssistantProviderException){Check(true,"Malformed provider envelope handled");}
var cs=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CAFE_DATABASE")){InitialCatalog="CafeAssistantChecks_"+Guid.NewGuid().ToString("N")};
await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs.ConnectionString).Options);
Process? server=null;
try{
 await DbSeeder.SeedAsync(db);
 var today=AssistantInsights.Today;var food=await db.Foods.FirstAsync();
 db.Bills.AddRange(new Bill{DateCheckIn=today,DateCheckOut=today,Status=1,TotalPrice=60000,HasRecordedPayment=true,BillInfos=[new BillInfo{IdFood=food.Id,Count=3,UnitPrice=25000,FoodNameSnapshot="Assistant demo coffee"}]},new Bill{DateCheckIn=today.AddDays(-1),DateCheckOut=today.AddDays(-1),Status=3,CancelledAt=today,RefundAmount=20000,TotalPrice=50000,HasRecordedPayment=true,BillInfos=[new BillInfo{IdFood=food.Id,Count=2,UnitPrice=25000}]},new Bill{DateCheckIn=today,Status=0,TotalPrice=900000});
 var ingredient=new Ingredient{Name="Assistant milk",Unit="ml",Quantity=12,MinQuantity=10};db.Ingredients.Add(ingredient);await db.SaveChangesAsync();
 db.Bills.Add(new Bill{DateCheckIn=today,Status=0,BillInfos=[new BillInfo{IdFood=food.Id,Count=1,SentCount=0,IngredientsJson=JsonSerializer.Serialize(new[]{new IngredientPortion(ingredient.Id,5)})}]});await db.SaveChangesAsync();
 var adminPrincipal=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Role,"Admin")},"test"));
 var insights=new AssistantInsights(db);var summary=await insights.Read(new("overview","today",true),adminPrincipal,"basic",null);
 Check(summary.Sections.Single(s=>s.Title=="Doanh thu").Cards[0].Value=="40.000 đ","Actual paid totals minus refund on its own date");
 Check(summary.Sections.Single(s=>s.Title=="Doanh thu").Cards.Single(c=>c.Label=="Kỳ liền trước").Value=="50.000 đ","Comparison uses preceding equal-length window");
 Check(summary.Sections.Single(s=>s.Title=="Đơn hàng").Cards[0].Value=="1","Unpaid and merged/closed excluded from paid order count");
 Check(summary.Sections.Single(s=>s.Title=="Đơn hàng").Cards.Single(c=>c.Label=="Kỳ liền trước").Value=="1","Order comparison uses preceding equal-length window");
 Check(summary.Sections.Single(s=>s.Title=="Món bán chạy").Rows[0].Value=="3 phần","Top foods use paid quantities only");
 Check(summary.Sections.Single(s=>s.Title=="Nguyên liệu sắp hết").Rows.Any(r=>r.Name=="Assistant milk"&&r.Value=="7 ml khả dụng"),"Held unsent recipes deducted from physical stock");
 var stockOnly=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim("Permission","INVENTORY_VIEW")},"test"));
 try{await insights.Read(new("sales","today",false),stockOnly,"basic",null);Check(false,"service permission bypass");}catch(UnauthorizedAccessException){Check(true,"Read service independently rejects forbidden revenue access");}
 var workerRole=await db.Roles.SingleAsync(r=>r.Name=="Cashier");
 db.Accounts.Add(new(){UserName="assistant.worker",DisplayName="Assistant test",IdRole=workerRole.Id,PassWord=BCrypt.Net.BCrypt.HashPassword("Assistant123")});await db.SaveChangesAsync();
 var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();var port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
 var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
 var psi=new ProcessStartInfo("dotnet"){WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
 foreach(var arg in new[]{Path.Combine(root,"CafeManagement.API/bin/Release/net8.0/CafeManagement.API.dll"),"--urls",$"http://localhost:{port}","--contentRoot",Path.Combine(root,"CafeManagement.API")})psi.ArgumentList.Add(arg);
 psi.Environment["ConnectionStrings__DefaultConnection"]=cs.ConnectionString;psi.Environment["LocalDb__UseNamedPipe"]="false";psi.Environment["ASPNETCORE_ENVIRONMENT"]="Development";psi.Environment["Assistant__Enabled"]="false";psi.Environment["Logging__LogLevel__Default"]="Error";
 server=Process.Start(psi)!;server.OutputDataReceived+=(_,_)=>{};server.ErrorDataReceived+=(_,_)=>{};server.BeginOutputReadLine();server.BeginErrorReadLine();
 using var admin=new HttpClient{BaseAddress=new Uri($"http://localhost:{port}/api/")};using var worker=new HttpClient{BaseAddress=admin.BaseAddress};using var anonymous=new HttpClient{BaseAddress=admin.BaseAddress};
 HttpResponseMessage? login=null;
 for(int n=0;n<80;n++){try{login=await admin.PostAsJsonAsync("Auth/login",new{userName="admin",passWord="admin123"});if(login.IsSuccessStatusCode)break;}catch(HttpRequestException){}await Task.Delay(250);}
 Check(login?.IsSuccessStatusCode==true,"API starts on isolated database");admin.DefaultRequestHeaders.Authorization=new("Bearer",(await login!.Content.ReadFromJsonAsync<JsonObject>())!["token"]!.GetValue<string>());
 var wl=await worker.PostAsJsonAsync("Auth/login",new{userName="assistant.worker",passWord="Assistant123"});Check(wl.IsSuccessStatusCode,"Worker login");worker.DefaultRequestHeaders.Authorization=new("Bearer",(await wl.Content.ReadFromJsonAsync<JsonObject>())!["token"]!.GetValue<string>());
 Check((await anonymous.GetAsync("Assistant/capabilities")).StatusCode==HttpStatusCode.Unauthorized,"Anonymous cannot open assistant");
 Check((await worker.GetAsync("Assistant/capabilities")).StatusCode==HttpStatusCode.Forbidden,"Default cashier cannot open AI without explicit grant");
 var perm=await db.Permissions.SingleAsync(p=>p.Code=="AI_VIEW");db.AccountPermissions.Add(new(){UserName="assistant.worker",IdPermission=perm.Id,Allowed=true});await db.SaveChangesAsync();
 var cap=await worker.GetFromJsonAsync<JsonObject>("Assistant/capabilities");Check(cap!["topics"]!.AsArray().Count==0,"AI access alone grants no business data");
 Check((await worker.PostAsJsonAsync("Assistant/ask",new{question="Doanh thu hôm nay"})).StatusCode==HttpStatusCode.Forbidden,"AI permission cannot bypass revenue rights");
 var stockPerm=await db.Permissions.SingleAsync(p=>p.Code=="INVENTORY_VIEW");db.AccountPermissions.Add(new(){UserName="assistant.worker",IdPermission=stockPerm.Id,Allowed=true});await db.SaveChangesAsync();
 var stockResponse=await worker.PostAsJsonAsync("Assistant/ask",new{question="Tình hình quán hôm nay"});var stockBody=await stockResponse.Content.ReadFromJsonAsync<JsonObject>();
 Check(stockResponse.IsSuccessStatusCode&&stockBody!["sections"]!.AsArray().Count==1&&stockBody["sections"]![0]!["title"]!.GetValue<string>()=="Nguyên liệu sắp hết","Overview contains only authorized stock facts");
 Check(!stockBody!.ToJsonString().Contains("40.000")&&!stockBody.ToJsonString().Contains("999999"),"Restricted answer omits revenue and unpaid bill amounts");
 db.AccountPermissions.Remove(await db.AccountPermissions.SingleAsync(p=>p.UserName=="assistant.worker"&&p.IdPermission==stockPerm.Id));await db.SaveChangesAsync();
 Check((await worker.PostAsJsonAsync("Assistant/ask",new{question="Nguyên liệu nào sắp hết?"})).StatusCode==HttpStatusCode.Forbidden,"Live revocation blocks existing token immediately");
 var answer=await admin.PostAsJsonAsync("Assistant/ask",new{question="Doanh thu hôm nay"});var body=await answer.Content.ReadFromJsonAsync<JsonObject>();
 Check(answer.IsSuccessStatusCode&&body!["mode"]!.GetValue<string>()=="basic"&&body["warning"]!=null,"Missing Gemini config honestly uses local basic mode");
 Check(body!["sections"]![0]!["cards"]![0]!["value"]!.GetValue<string>()=="40.000 đ","HTTP response carries verified real totals");
 Check(!body.ToJsonString().Contains("PassWord")&&!body.ToJsonString().Contains("ApiKey")&&!body.ToJsonString().Contains("CustomerPhone"),"Response contains no credentials or personal customer data");
 var unsupported=await admin.PostAsJsonAsync("Assistant/ask",new{question="Xóa đơn hàng giúp tôi"});var unsupportedBody=await unsupported.Content.ReadFromJsonAsync<JsonObject>();Check(unsupported.IsSuccessStatusCode&&unsupportedBody!["sections"]!.AsArray().Count==0,"Write question returns guidance without accessing data");
 Check((await admin.PostAsJsonAsync("Assistant/ask",new{question=new string('a',501)})).StatusCode==HttpStatusCode.BadRequest,"Oversized question validated");
 var limiting=new List<HttpStatusCode>();for(int i=0;i<5;i++)limiting.Add((await admin.PostAsJsonAsync("Assistant/ask",new{question="Doanh thu hôm nay"})).StatusCode);
 Check(limiting.Contains(HttpStatusCode.TooManyRequests),"Per-account assistant rate limit protects free-tier quota");
 Check(await db.Bills.CountAsync()==4&&await db.Ingredients.Where(i=>i.Id==ingredient.Id).Select(i=>i.Quantity).SingleAsync()==12,"Assistant never changes bills or physical stock");
 Console.WriteLine($"ALL {checks} ASSISTANT CHECKS PASSED");
}finally{if(server!=null){if(!server.HasExited)server.Kill(true);await server.WaitForExitAsync();server.Dispose();}await db.Database.EnsureDeletedAsync();}
class FakeGemini:HttpMessageHandler {
 public string Text="";public HttpStatusCode Status=HttpStatusCode.OK;public bool Malformed;public Uri? Uri;public string? HeaderKey;public string? Body;
 protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){Uri=request.RequestUri;HeaderKey=request.Headers.GetValues("x-goog-api-key").Single();Body=await request.Content!.ReadAsStringAsync(ct);return new(Status){Content=JsonContent.Create(Malformed?new{invalid=true}:(object)new{candidates=new[]{new{finishReason="STOP",content=new{parts=new[]{new{text=Text}}}}}})};}
}
