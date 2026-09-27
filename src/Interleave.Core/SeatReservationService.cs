// INTERLEAVE — Last Seat experiment
// This is the REPAIRED implementation (M1).
//
// Root-cause fix: the check-then-act atomicity gap is closed by holding a lock
// across the entire read-check-decrement-add sequence.  No thread can observe
// or modify _availableSeats or _reservations between the guard check and the
// write that follows it.
//
// Deterministic identity: reservation IDs are now derived from a sequence
// counter (_nextSeq), incremented under the service lock, instead of
// Guid.NewGuid(). This eliminates the uncontrolled data-nondeterminism that
// Coyote warned about ("System.Guid.NewGuid introduces data non-determinism")
// and makes reservation identities stable
// across Coyote's controlled replays. The identity is unique within a service
// instance and carries the holder name for readability.
//
// Safe reads: AvailableSeats and Reservations take the same lock so callers
// cannot observe torn state while a reservation is in flight.
//
// No artificial sleep, no test-only path, no hard-coded schedule.

using System.Threading;
using Microsoft.Coyote.Runtime;

namespace Interleave.Core;

/// <summary>
/// Represents a single seat reservation.
/// </summary>
public sealed record Reservation(string ReservationId, string HolderId);

/// <summary>
/// Manages seat reservations for a fixed-capacity venue.
/// Thread-safe: all mutations and reads are protected by a single lock.
/// Reservation IDs are deterministic (counter-based) so that Coyote's
/// controlled scheduler can replay an exact trace without data-nondeterminism.
/// </summary>
public sealed class SeatReservationService
{
    private readonly object _lock = new();
    private int _availableSeats;
    private readonly List<Reservation> _reservations = new();
    private int _nextSeq;   // monotonic per-service sequence, protected by _lock

    public SeatReservationService(int capacity)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _availableSeats = capacity;
    }

    /// <summary>
    /// Number of seats still available (not yet reserved).
    /// </summary>
    public int AvailableSeats
    {
        get
        {
            lock (_lock)
            {
                return _availableSeats;
            }
        }
    }

    /// <summary>
    /// Snapshot of all confirmed reservations at the moment of the call.
    /// Returns a new list so the caller cannot mutate internal state.
    /// </summary>
    public IReadOnlyList<Reservation> Reservations
    {
        get
        {
            lock (_lock)
            {
                return _reservations.ToList();
            }
        }
    }

    /// <summary>
    /// Attempts to reserve a seat for the given holder.
    /// Returns the new Reservation on success, or null if no seat is available.
    ///
    /// FIX: The entire check-decrement-add sequence is executed while holding
    /// <c>_lock</c>.  The two SchedulingPoint.Interleave() boundaries are
    /// retained inside the lock so Coyote's scheduler can still explore
    /// interleavings at those points — but any thread that arrives at the lock
    /// while another holds it is blocked until the critical section completes,
    /// making it impossible for two callers to both pass the capacity guard.
    /// </summary>
    public Reservation? TryReserve(string holderId)
    {
        lock (_lock)
        {
            // Coyote scheduling boundary inside the lock: explores context-switches
            // at this point, but any competing thread is blocked on lock acquisition.
            SchedulingPoint.Interleave();
            if (_availableSeats <= 0)
            {
                return null;
            }

            SchedulingPoint.Interleave();
            _availableSeats--;

            // Deterministic identity: "<seq>-<holderId>" — unique per instance,
            // no external randomness, fully reproducible under Coyote replay.
            var reservationId = $"{++_nextSeq}-{holderId}";
            var reservation = new Reservation(
                ReservationId: reservationId,
                HolderId: holderId);

            _reservations.Add(reservation);
            return reservation;
        }
    }
}
