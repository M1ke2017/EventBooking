
BEGIN;

CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE TABLE IF NOT EXISTS public.reservation_occupancy
(
    reservation_id uuid PRIMARY KEY,
    resource_id uuid NOT NULL,
    reserved_period tstzrange NOT NULL,

    CONSTRAINT reservation_period_valid
        CHECK (NOT isempty(reserved_period)),

    CONSTRAINT reservation_occupancy_no_overlap
        EXCLUDE USING gist
        (
            resource_id WITH =,
            reserved_period WITH &&
        )
);

-- Blokujemy równoczesne zapisy na czas inicjalizacji.
LOCK TABLE public.mt_doc_reservation
    IN SHARE ROW EXCLUSIVE MODE;

-- Przenosimy aktualne aktywne rezerwacje.
INSERT INTO public.reservation_occupancy
(
    reservation_id,
    resource_id,
    reserved_period
)
SELECT
    id,
    (data->>'ResourceId')::uuid,
    tstzrange(
        (data->>'Start')::timestamptz,
        (data->>'End')::timestamptz,
        '[)'
    )
FROM public.mt_doc_reservation
WHERE COALESCE(
    (data->>'IsCancelled')::boolean,
    false
) = false
ON CONFLICT (reservation_id)
DO UPDATE SET
    resource_id = EXCLUDED.resource_id,
    reserved_period = EXCLUDED.reserved_period;

-- Synchronizacja zajętości z projekcją Marten.
CREATE OR REPLACE FUNCTION
public.sync_reservation_occupancy()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        DELETE FROM public.reservation_occupancy
        WHERE reservation_id = OLD.id;

        RETURN OLD;
    END IF;

    IF COALESCE(
        (NEW.data->>'IsCancelled')::boolean,
        false
    ) = true THEN

        DELETE FROM public.reservation_occupancy
        WHERE reservation_id = NEW.id;

    ELSE

        INSERT INTO public.reservation_occupancy
        (
            reservation_id,
            resource_id,
            reserved_period
        )
        VALUES
        (
            NEW.id,
            (NEW.data->>'ResourceId')::uuid,
            tstzrange(
                (NEW.data->>'Start')::timestamptz,
                (NEW.data->>'End')::timestamptz,
                '[)'
            )
        )
        ON CONFLICT (reservation_id)
        DO UPDATE SET
            resource_id = EXCLUDED.resource_id,
            reserved_period = EXCLUDED.reserved_period;

    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS
    trg_reservation_occupancy
ON public.mt_doc_reservation;

CREATE TRIGGER trg_reservation_occupancy
AFTER INSERT OR UPDATE OR DELETE
ON public.mt_doc_reservation
FOR EACH ROW
EXECUTE FUNCTION public.sync_reservation_occupancy();

COMMIT;
