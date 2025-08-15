using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace TestFoodyProject
{
    public class FoodytTests
    {
        private RestClient client;
        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86/api/";
        private const string userName = "eniHD";
        private const string passWord = "123456";
        private static string createdFoodyId;
        
        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken(userName, passWord);
            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string userName, string passWord)
        {
            RestClient authClient = new RestClient(BaseUrl);
            var request = new RestRequest("User/Authentication");
            request.AddJsonBody(new
            {
                userName,
                passWord
            });

            var response = authClient.Execute(request, Method.Post);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("AccessToken is null or empty");
                }
                return token;

            }
            else
            {
                throw new InvalidOperationException($"Authentication failed: {response.StatusCode} - {response.Content}");
            }
        }

        
        [Test, Order(1)]
        public void CreateNewFood_ShouldSuccseed()
        {
            var food = new
            {
                Name = "New Food",
                Description = "Delicious new food item",
                Url = ""
            };

            var request = new RestRequest("Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Create food request failed");
            var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdFoodyId = content.GetProperty("foodId").GetString() ?? string.Empty;

            Assert.That(createdFoodyId, Is.Not.Null.Or.Empty, "Food Id should not be null or empty");
        }


        [Test, Order(2)]
        public void EditTitleFood_ShouldSuccseed()
        {
            var changes = new[]
            {
                new {path = "/name", op = "replace", value = "Updated food name "  }
            };

            var request = new RestRequest($"Food/Edit/{createdFoodyId}", Method.Patch);

            request.AddJsonBody(changes);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }


        [Test, Order(3)]
        public void GetAllFood_ShouldSucceed()
        {
            var request = new RestRequest("Food/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.OK));

            var foods = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(foods, Is.Not.Empty);
        }


        
        [Test, Order(4)]
        public void DeleteEditedFood_ShouldSuccseed()
        {
            var request = new RestRequest($"Food/Delete/{createdFoodyId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }

        
        [Test, Order(5)]
        public void CreateFood_Without_Fields()
        {
            var food = new
            {
                Name = "",
                Description = ""
            };

            var request = new RestRequest("Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.BadRequest));
        }


        [Test, Order(6)]
        public void Edit_NonExistingFood()
        {
            string fakeId = "123";
            var changes = new[]
            {
                new{path = "/name" , op = "replace", value = "New Title" }
            };
            var request = new RestRequest($"Food/Edit/{fakeId}" , Method.Patch);
            request.AddJsonBody(changes);

            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No food revues..."));

        }

        [Test, Order(7)]
        public void Delete_NonExistingFood()
        {
            string fakeID = "1234";

            var request = new RestRequest($"Food/Delete/{fakeID}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            client?.Dispose();
        }

    }
}