using NUnit.Framework;
using PixelVault.IntegrationTests.Models;
using static RestAssured.Dsl;

namespace PixelVault.IntegrationTests.Answers;

[TestFixture]
public class Answers03 : CatalogApiTestBase
{
    /*
     * TODO: retrieve the game with id 1 by sending a GET request to {CatalogBaseUrl}/games/1
     * and deserialize the response body into an object of type Game (see Models/Game.cs)
     * using DeserializeTo<Game>().
     *
     * Then use regular NUnit assertions to verify that:
     * - the title is "Ashes of Valdren"
     * - the base price (in the nested Pricing object) is 59.99
     * - the list of platforms contains "PlayStation"
     */
    [Test]
    public void GetGame1_DeserializeToGame_VerifyProperties()
    {
        var game = Given()
            .When()
            .Get($"{CatalogBaseUrl}/games/1")
            .Then()
            .StatusCode(200)
            .DeserializeTo<Game>();

        Assert.That(game!.Title, Is.EqualTo("Ashes of Valdren"));
        Assert.That(game.Pricing.BasePrice, Is.EqualTo(59.99m));
        Assert.That(game.Platforms, Does.Contain("PlayStation"));
    }

    /*
     * TODO: create a ReservationRequest object (see Models/Reservation.cs) with:
     * - OrderReference: "ANSWERS03-MODEL"
     * - Items: one ReservationRequestItem for game 1, quantity 2
     *
     * Send it as the request body in a POST request to {CatalogBaseUrl}/reservations
     * and verify that the response status code is 201 (Created)
     * and that the value of the status property in the response body is "Active".
     */
    [Test]
    public void CreateReservation_SerializeModel_ShouldBeCreated()
    {
        var reservationRequest = new ReservationRequest
        {
            OrderReference = "ANSWERS03-MODEL",
            Items =
            [
                new ReservationRequestItem { GameId = 1, Quantity = 2 }
            ]
        };

        Given()
            .Body(reservationRequest)
            .When()
            .Post($"{CatalogBaseUrl}/reservations")
            .Then()
            .AssertThat()
            .StatusCode(201)
            .And()
            .Body("$.status", NHamcrest.Is.EqualTo("Active"));
    }

    /*
     * TODO: do the same as in the previous exercise, but this time create the request body
     * as an anonymous object (new { ... }) instead of using the ReservationRequest class.
     * Use "ANSWERS03-ANONYMOUS" as the order reference.
     *
     * Note: the order reference has to be different from the one in the previous exercise.
     * Reserving twice for the same order reference returns the existing reservation with a 200 OK
     * instead of creating a new one.
     */
    [Test]
    public void CreateReservation_SerializeAnonymousObject_ShouldBeCreated()
    {
        var reservationRequest = new
        {
            orderReference = "ANSWERS03-ANONYMOUS",
            items = new[]
            {
                new { gameId = 1, quantity = 2 }
            }
        };

        Given()
            .Body(reservationRequest)
            .When()
            .Post($"{CatalogBaseUrl}/reservations")
            .Then()
            .AssertThat()
            .StatusCode(201)
            .And()
            .Body("$.status", NHamcrest.Is.EqualTo("Active"));
    }

    /*
     * TODO: create a reservation for order reference "ANSWERS03-ROUNDTRIP" with two items:
     * - game 1 (Ashes of Valdren), quantity 2
     * - game 20 (Echoes of the Void), quantity 1
     * Send it in a POST request to {CatalogBaseUrl}/reservations and verify that the status code is 201.
     *
     * Then retrieve the reservation by sending a GET request to {CatalogBaseUrl}/reservations/ANSWERS03-ROUNDTRIP
     * and deserialize the response body into an object of type Reservation.
     *
     * Verify that:
     * - the order reference is the same as in the request you sent
     * - the status is "Active"
     * - the number of items in the reservation is the same as in the request you sent
     */
    [Test]
    public void CreateAndRetrieveReservation_VerifyRetrievedReservationMatchesRequest()
    {
        var reservationRequest = new ReservationRequest
        {
            OrderReference = "ANSWERS03-ROUNDTRIP",
            Items =
            [
                new ReservationRequestItem { GameId = 1, Quantity = 2 },
                new ReservationRequestItem { GameId = 20, Quantity = 1 }
            ]
        };

        Given()
            .Body(reservationRequest)
            .When()
            .Post($"{CatalogBaseUrl}/reservations")
            .Then()
            .AssertThat()
            .StatusCode(201);

        var reservation = Given()
            .When()
            .Get($"{CatalogBaseUrl}/reservations/{reservationRequest.OrderReference}")
            .Then()
            .StatusCode(200)
            .DeserializeTo<Reservation>();

        Assert.That(reservation!.OrderReference, Is.EqualTo(reservationRequest.OrderReference));
        Assert.That(reservation.Status, Is.EqualTo("Active"));
        Assert.That(reservation.Items, Has.Count.EqualTo(reservationRequest.Items.Count));
    }
}
