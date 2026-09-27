// INTERLEAVE — Sequential baseline tests
//
// These tests run single-threaded and establish the service's sequential behavior.
// The six original behavior tests also pass on the baseline because its defect
// requires concurrent scheduling. The deterministic-ID and detached-snapshot
// regressions assert guarantees introduced by the repair.
//
// If any of these fail, the implementation has a basic logic error that is separate
// from the concurrency bug, and that must be fixed before the concurrency experiment
// is meaningful.

using Interleave.Core;
using Xunit;

namespace Interleave.Tests.Sequential;

public sealed class SeatReservationSequentialTests
{
    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public void Reserving_available_seat_succeeds()
    {
        var service = new SeatReservationService(capacity: 1);

        var result = service.TryReserve("alice");

        Assert.NotNull(result);
        Assert.Equal("alice", result.HolderId);
        Assert.Equal("1-alice", result.ReservationId);
    }

    [Fact]
    public void Reservation_ids_follow_success_sequence_within_service()
    {
        var service = new SeatReservationService(capacity: 2);

        var first = service.TryReserve("alice");
        var second = service.TryReserve("bob");

        Assert.Equal("1-alice", first!.ReservationId);
        Assert.Equal("2-bob", second!.ReservationId);
    }

    [Fact]
    public void Reservations_returns_a_detached_snapshot()
    {
        var service = new SeatReservationService(capacity: 2);
        service.TryReserve("alice");

        var snapshot = service.Reservations;
        service.TryReserve("bob");

        Assert.Single(snapshot);
        Assert.Equal(2, service.Reservations.Count);
    }

    [Fact]
    public void After_successful_reservation_available_seats_decrements()
    {
        var service = new SeatReservationService(capacity: 1);

        service.TryReserve("alice");

        Assert.Equal(0, service.AvailableSeats);
    }

    [Fact]
    public void After_successful_reservation_it_appears_in_the_list()
    {
        var service = new SeatReservationService(capacity: 1);

        var reservation = service.TryReserve("alice");

        Assert.Single(service.Reservations);
        Assert.Equal(reservation!.ReservationId, service.Reservations[0].ReservationId);
    }

    // ── Boundary: no seat available ───────────────────────────────────────────

    [Fact]
    public void Reserving_when_no_seat_available_returns_null()
    {
        var service = new SeatReservationService(capacity: 1);
        service.TryReserve("alice"); // consume the only seat

        var result = service.TryReserve("bob");

        Assert.Null(result);
    }

    [Fact]
    public void Two_sequential_callers_produce_exactly_one_reservation()
    {
        var service = new SeatReservationService(capacity: 1);

        service.TryReserve("alice");
        service.TryReserve("bob");

        // Business invariant: successful reservations must not exceed capacity
        Assert.True(
            service.Reservations.Count <= 1,
            $"Expected at most 1 reservation but found {service.Reservations.Count}.");
    }

    [Fact]
    public void Zero_capacity_venue_rejects_all_reservations()
    {
        var service = new SeatReservationService(capacity: 0);

        var result = service.TryReserve("alice");

        Assert.Null(result);
        Assert.Empty(service.Reservations);
    }
}
