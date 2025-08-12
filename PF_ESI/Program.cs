
namespace PF_ESI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.


            var MyAllowSpecificOrigins = "_myspecificorigins";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                                  policy =>
                                  {
                                      policy.WithOrigins("http://192.168.29.29:5211;https://192.168.29.29:7192; https://enchanting-otter-e59375.netlify.app;http://localhost:3000; https://192.168.29.128:3000; https://192.168.29.29:3000")// "http://192.168.29.29:5211;http://192.168.29.29:7192"
                                      .AllowAnyHeader()
                                      .AllowAnyOrigin()
                                      .AllowAnyMethod();

                                  });
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseCors(MyAllowSpecificOrigins);
            app.MapControllers();

            var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
            app.Urls.Add($"http://0.0.0.0:{port}");

            app.Run();
        }
    }
}
