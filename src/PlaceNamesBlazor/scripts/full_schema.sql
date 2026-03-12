-- Full schema for PlaceNames DB (mirrors Flask/Alembic migrations).
-- Run idempotently at startup. Database must already exist (e.g. place_names_db).

-- 1. fylker
CREATE TABLE IF NOT EXISTS fylker (
    fylke_id     SERIAL PRIMARY KEY,
    fylke_navn   VARCHAR(50) NOT NULL UNIQUE,
    created_at   TIMESTAMP NULL
);

-- 2. rapportoer (status column added later via DO block)
CREATE TABLE IF NOT EXISTS rapportoer (
    rapportoer_id      SERIAL PRIMARY KEY,
    initialer          VARCHAR(20) NOT NULL UNIQUE,
    fornavn_etternavn  VARCHAR(100) NOT NULL,
    telefon            VARCHAR(20) NULL,
    epost              VARCHAR(100) NOT NULL,
    medlemsklubb       VARCHAR(100) NULL,
    created_at         TIMESTAMP NULL,
    updated_at         TIMESTAMP NULL
);

-- 3. stempeltyper
CREATE TABLE IF NOT EXISTS stempeltyper (
    stempeltype_id        SERIAL PRIMARY KEY,
    hovedstempeltype      VARCHAR(10) NOT NULL UNIQUE,
    stempeltype_full_tekst VARCHAR(100) NOT NULL,
    maanedsangivelse_type VARCHAR(1) NOT NULL CHECK (maanedsangivelse_type IN ('A', 'L', 'T')),
    stempelutfoerelse     VARCHAR(1) NOT NULL CHECK (stempelutfoerelse IN ('G', 'S')),
    skrifttype            VARCHAR(50) NULL,
    created_at            TIMESTAMP NULL,
    updated_at            TIMESTAMP NULL
);

-- 4. kommuner
CREATE TABLE IF NOT EXISTS kommuner (
    kommune_id   SERIAL PRIMARY KEY,
    kommunenavn  VARCHAR(50) NOT NULL,
    fylke_id     INTEGER NOT NULL REFERENCES fylker(fylke_id) ON DELETE RESTRICT,
    created_at   TIMESTAMP NULL,
    UNIQUE(kommunenavn, fylke_id)
);
CREATE INDEX IF NOT EXISTS idx_kommunenavn_ilike ON kommuner(kommunenavn);

-- 5. underkategori_stempeltyper
CREATE TABLE IF NOT EXISTS underkategori_stempeltyper (
    underkategori_id        SERIAL PRIMARY KEY,
    stempeltype_id          INTEGER NOT NULL REFERENCES stempeltyper(stempeltype_id) ON DELETE CASCADE,
    underkategori           VARCHAR(10) NOT NULL,
    underkategori_full_tekst VARCHAR(100) NULL,
    created_at              TIMESTAMP NULL,
    updated_at              TIMESTAMP NULL,
    UNIQUE(stempeltype_id, underkategori)
);
CREATE INDEX IF NOT EXISTS idx_underkategori_code ON underkategori_stempeltyper(underkategori);
CREATE INDEX IF NOT EXISTS idx_underkategori_stempeltype_id ON underkategori_stempeltyper(stempeltype_id);

-- 6. users
CREATE TABLE IF NOT EXISTS users (
    user_id       SERIAL PRIMARY KEY,
    first_name    VARCHAR(50) NOT NULL,
    last_name     VARCHAR(50) NOT NULL,
    middle_name   VARCHAR(50) NULL,
    email         VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role          VARCHAR(20) NOT NULL CHECK (role IN ('guest', 'registered', 'superuser', 'admin')),
    username      VARCHAR(50) NULL UNIQUE,
    telephone     VARCHAR(20) NULL,
    rapportoer_id INTEGER NULL REFERENCES rapportoer(rapportoer_id) ON DELETE SET NULL,
    is_active     BOOLEAN NULL,
    last_login    TIMESTAMP NULL,
    created_at    TIMESTAMP NULL,
    updated_at    TIMESTAMP NULL
);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_role ON users(role);

-- 7. poststeder
CREATE TABLE IF NOT EXISTS poststeder (
    poststed_id           SERIAL PRIMARY KEY,
    postnummer            VARCHAR(4) NULL,
    poststed_navn         VARCHAR(50) NOT NULL,
    tidligere_navn        VARCHAR(50) NULL,
    poststed_fra_dato      VARCHAR(15) NULL,
    poststed_til_dato      VARCHAR(15) NULL,
    tidligere_poststed_id  INTEGER NULL REFERENCES poststeder(poststed_id) ON DELETE SET NULL,
    kommune_id             INTEGER NULL REFERENCES kommuner(kommune_id) ON DELETE SET NULL,
    kommentarer            VARCHAR(100) NULL,
    created_at             TIMESTAMP NULL,
    updated_at             TIMESTAMP NULL
);

-- 8. stempler (underkategori_id added in next migration, added below via DO block)
CREATE TABLE IF NOT EXISTS stempler (
    stempel_id     SERIAL PRIMARY KEY,
    poststed_id    INTEGER NOT NULL REFERENCES poststeder(poststed_id) ON DELETE RESTRICT,
    stempeltype_id INTEGER NOT NULL REFERENCES stempeltyper(stempeltype_id) ON DELETE RESTRICT,
    stempeltekst_oppe VARCHAR(40) NOT NULL,
    stempeltekst_nede VARCHAR(20) NULL,
    stempeltekst_midt VARCHAR(20) NULL,
    stempelgravoer  VARCHAR(10) NULL,
    dato_fra_gravoer DATE NULL,
    dato_fra_intendantur_til_overordnet_postkontor DATE NULL,
    dato_fra_overordnet_postkontor DATE NULL,
    dato_for_innlevering_til_overordnet_postkontor DATE NULL,
    dato_innlevert_intendantur DATE NULL,
    tapsmelding    VARCHAR(30) NULL,
    stempeldiameter NUMERIC(5,1) NULL,
    bokstavhoeyde   NUMERIC(5,1) NULL,
    andre_maal      VARCHAR(50) NULL,
    stempelfarge    VARCHAR(30) NULL,
    reparasjoner    VARCHAR(50) NULL,
    dato_avtrykk_i_pm VARCHAR(40) NULL,
    kommentar       VARCHAR(256) NULL,
    created_at      TIMESTAMP NULL,
    updated_at      TIMESTAMP NULL
);
CREATE INDEX IF NOT EXISTS idx_stempeltekst_nede_ilike ON stempler(stempeltekst_nede);
CREATE INDEX IF NOT EXISTS idx_stempeltekst_oppe_ilike ON stempler(stempeltekst_oppe);
CREATE INDEX IF NOT EXISTS idx_stempler_poststed_id ON stempler(poststed_id);
CREATE INDEX IF NOT EXISTS idx_stempler_stempeltype_id ON stempler(stempeltype_id);

-- 9. bruksperioder
CREATE TABLE IF NOT EXISTS bruksperioder (
    bruksperiode_id                  SERIAL PRIMARY KEY,
    stempel_id                       INTEGER NOT NULL REFERENCES stempler(stempel_id) ON DELETE CASCADE,
    bruksperiode_fra                 VARCHAR(20) NULL,
    bruksperiode_til                 VARCHAR(20) NULL,
    dato_foerste_kjente_bruksdato    VARCHAR(15) NULL,
    dato_foerste_kjente_bruksdato_tillegg DATE NULL,
    rapportoer_id_foerste_bruksdato  INTEGER NULL REFERENCES rapportoer(rapportoer_id) ON DELETE SET NULL,
    dato_siste_kjente_bruksdato      VARCHAR(15) NULL,
    dato_siste_kjente_bruksdato_tillegg DATE NULL,
    rapportoer_id_siste_bruksdato    INTEGER NULL REFERENCES rapportoer(rapportoer_id) ON DELETE SET NULL,
    kommentarer                      VARCHAR(100) NULL,
    created_at                       TIMESTAMP NULL,
    updated_at                       TIMESTAMP NULL,
    CHECK ((bruksperiode_fra IS NULL OR bruksperiode_til IS NULL) OR (bruksperiode_fra < bruksperiode_til))
);
CREATE INDEX IF NOT EXISTS idx_bruksperioder_stempel_id ON bruksperioder(stempel_id);
CREATE INDEX IF NOT EXISTS idx_bruksperioder_rapportoer_foerste ON bruksperioder(rapportoer_id_foerste_bruksdato);
CREATE INDEX IF NOT EXISTS idx_bruksperioder_rapportoer_siste ON bruksperioder(rapportoer_id_siste_bruksdato);

-- 10. stempelbilder
CREATE TABLE IF NOT EXISTS stempelbilder (
    bilde_id       SERIAL PRIMARY KEY,
    stempel_id     INTEGER NOT NULL REFERENCES stempler(stempel_id) ON DELETE CASCADE,
    bilde_path     VARCHAR(255) NOT NULL,
    bilde_filnavn  VARCHAR(255) NULL,
    er_primær      BOOLEAN NULL,
    beskrivelse    TEXT NULL,
    opplastet_dato TIMESTAMP NULL,
    opplastet_av   VARCHAR(100) NULL,
    created_at     TIMESTAMP NULL
);
CREATE INDEX IF NOT EXISTS idx_stempelbilder_stempel_id ON stempelbilder(stempel_id);
CREATE INDEX IF NOT EXISTS idx_stempelbilder_er_primær ON stempelbilder(stempel_id, er_primær) WHERE er_primær = true;

-- 11. bruksperioder_bilder
CREATE TABLE IF NOT EXISTS bruksperioder_bilder (
    bilde_id         SERIAL PRIMARY KEY,
    bruksperiode_id  INTEGER NOT NULL REFERENCES bruksperioder(bruksperiode_id) ON DELETE CASCADE,
    bilde_path       VARCHAR(255) NOT NULL,
    bilde_filnavn    VARCHAR(255) NULL,
    bilde_nummer     INTEGER NOT NULL CHECK (bilde_nummer IN (1, 2)),
    beskrivelse      TEXT NULL,
    opplastet_dato   TIMESTAMP NULL,
    opplastet_av     VARCHAR(100) NULL,
    created_at       TIMESTAMP NULL,
    UNIQUE(bruksperiode_id, bilde_nummer)
);
CREATE INDEX IF NOT EXISTS idx_bruksperioder_bilder_bruksperiode_id ON bruksperioder_bilder(bruksperiode_id);

-- 12. rapporteringshistorikk
CREATE TABLE IF NOT EXISTS rapporteringshistorikk (
    rapporteringshistorikk_id    SERIAL PRIMARY KEY,
    stempel_id                   INTEGER NOT NULL REFERENCES stempler(stempel_id) ON DELETE CASCADE,
    bruksperiode_id              INTEGER NOT NULL REFERENCES bruksperioder(bruksperiode_id) ON DELETE CASCADE,
    rapporteringsdato            DATE NOT NULL,
    rapportoer_id                INTEGER NOT NULL REFERENCES rapportoer(rapportoer_id) ON DELETE RESTRICT,
    rapportering_foerste_siste_dato VARCHAR(1) NOT NULL CHECK (rapportering_foerste_siste_dato IN ('F', 'S')),
    dato_for_rapportert_avtrykk  VARCHAR(15) NOT NULL,
    godkjent_forkastet           VARCHAR(1) NULL CHECK (godkjent_forkastet IN ('G', 'F')),
    besluttet_dato               DATE NULL,
    initialer_beslutter         VARCHAR(20) NULL,
    created_at                   TIMESTAMP NULL,
    updated_at                   TIMESTAMP NULL
);
CREATE INDEX IF NOT EXISTS idx_rapporteringshistorikk_stempel_id ON rapporteringshistorikk(stempel_id);
CREATE INDEX IF NOT EXISTS idx_rapporteringshistorikk_rapportoer_id ON rapporteringshistorikk(rapportoer_id);
CREATE INDEX IF NOT EXISTS idx_rapporteringshistorikk_bruksperiode_id ON rapporteringshistorikk(bruksperiode_id);
CREATE INDEX IF NOT EXISTS idx_rapporteringshistorikk_godkjent_forkastet ON rapporteringshistorikk(godkjent_forkastet);
CREATE INDEX IF NOT EXISTS idx_rapporteringshistorikk_besluttet_dato ON rapporteringshistorikk(besluttet_dato);

-- 13. rapporteringshistorikk_bilder
CREATE TABLE IF NOT EXISTS rapporteringshistorikk_bilder (
    bilde_id                    SERIAL PRIMARY KEY,
    rapporteringshistorikk_id   INTEGER NOT NULL REFERENCES rapporteringshistorikk(rapporteringshistorikk_id) ON DELETE CASCADE,
    bilde_path                  VARCHAR(255) NOT NULL,
    bilde_filnavn               VARCHAR(255) NULL,
    beskrivelse                 TEXT NULL,
    opplastet_dato              TIMESTAMP NULL,
    created_at                  TIMESTAMP NULL
);
CREATE INDEX IF NOT EXISTS idx_rapporteringshistorikk_bilder_rapporteringshistorikk_id ON rapporteringshistorikk_bilder(rapporteringshistorikk_id);

-- 14. audit_logs
CREATE TABLE IF NOT EXISTS audit_logs (
    audit_id           SERIAL PRIMARY KEY,
    actor_id           INTEGER NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    actor_email        VARCHAR(100) NOT NULL,
    actor_role         VARCHAR(20) NOT NULL,
    action_type        VARCHAR(50) NOT NULL,
    target_type        VARCHAR(50) NULL,
    target_id          INTEGER NULL,
    target_description VARCHAR(255) NULL,
    details            JSONB NULL,
    ip_address         VARCHAR(45) NULL,
    user_agent         VARCHAR(512) NULL,
    created_at         TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
);
CREATE INDEX IF NOT EXISTS idx_audit_logs_actor_id ON audit_logs(actor_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_action_type ON audit_logs(action_type);
CREATE INDEX IF NOT EXISTS idx_audit_logs_target_type ON audit_logs(target_type);
CREATE INDEX IF NOT EXISTS idx_audit_logs_target_id ON audit_logs(target_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON audit_logs(created_at);

-- Add user_agent to existing audit_logs (idempotent)
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'audit_logs' AND column_name = 'user_agent'
  ) THEN
    ALTER TABLE audit_logs ADD COLUMN user_agent VARCHAR(512) NULL;
  END IF;
END $$;

-- Append-only: block UPDATE/DELETE/TRUNCATE on audit_logs
DROP TRIGGER IF EXISTS audit_logs_append_only_trigger ON audit_logs;
DROP TRIGGER IF EXISTS audit_logs_reject_truncate_trigger ON audit_logs;
DROP FUNCTION IF EXISTS audit_logs_reject_update_delete();
CREATE FUNCTION audit_logs_reject_update_delete() RETURNS TRIGGER AS $$
BEGIN
  RAISE EXCEPTION 'audit_logs is append-only; updates and deletes are not allowed.';
  RETURN NULL;
END;
$$ LANGUAGE plpgsql;
CREATE TRIGGER audit_logs_append_only_trigger
  BEFORE UPDATE OR DELETE ON audit_logs
  FOR EACH ROW EXECUTE FUNCTION audit_logs_reject_update_delete();
CREATE TRIGGER audit_logs_reject_truncate_trigger
  BEFORE TRUNCATE ON audit_logs
  FOR EACH STATEMENT EXECUTE FUNCTION audit_logs_reject_update_delete();

-- 15. Add underkategori_id to stempler if missing (migration 371ccccc4f79)
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'stempler' AND column_name = 'underkategori_id'
  ) THEN
    ALTER TABLE stempler ADD COLUMN underkategori_id INTEGER NULL REFERENCES underkategori_stempeltyper(underkategori_id) ON DELETE SET NULL;
  END IF;
END $$;

-- 16. Add status to rapportoer if missing (migration 3f9833cef78c / add_rapportoer_status.sql)
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'rapportoer' AND column_name = 'status'
  ) THEN
    ALTER TABLE rapportoer ADD COLUMN status VARCHAR(20) NULL;
    UPDATE rapportoer SET status = 'approved' WHERE status IS NULL;
    ALTER TABLE rapportoer ALTER COLUMN status SET NOT NULL;
    ALTER TABLE rapportoer ALTER COLUMN status SET DEFAULT 'pending';
  END IF;
END $$;

-- 17. Seed fylker (from initial migration)
INSERT INTO fylker (fylke_navn) VALUES
    ('Akershus'),
    ('Aust-Agder'),
    ('Bergen'),
    ('Buskerud'),
    ('Christiania/Oslo'),
    ('Finnmark'),
    ('Hedmark'),
    ('Hordaland'),
    ('Møre & Romsdal'),
    ('Nord-Trøndelag'),
    ('Nordland'),
    ('Oppland'),
    ('Rogaland'),
    ('Sogn & Fjordane'),
    ('Sør-Trøndelag'),
    ('Svalbard'),
    ('Telemark'),
    ('Troms'),
    ('Vest-Agder'),
    ('Vestfold'),
    ('Østfold')
ON CONFLICT (fylke_navn) DO NOTHING;

-- 18. Seed stempeltyper (from initial migration)
INSERT INTO stempeltyper (hovedstempeltype, stempeltype_full_tekst, maanedsangivelse_type, stempelutfoerelse) VALUES
    ('I', 'Enringsstempel', 'L', 'S'),
    ('SL', 'Sveitserstempel med latinsk månedsangivelse', 'L', 'S'),
    ('SA', 'Sveitserstempel med arabisk månedsangivelse', 'A', 'S'),
    ('TL', 'Toringsstempel med tverrbjelke og latinsk månedsangivelse', 'L', 'S'),
    ('TA', 'Toringsstempel med tverrbjelke og arabisk månedsangivelse', 'A', 'S'),
    ('IIL', 'Toringsstempel med latinsk månedsangivelse', 'L', 'S'),
    ('IIA', 'Toringsstempel med arabisk månedsangivelse', 'A', 'S'),
    ('IIT', 'Toringsstempel med tognummer', 'L', 'S'),
    ('I20', 'Moderne enringsstempel med postnummer', 'A', 'S'),
    ('I22', 'Moderne enringsstempel med postnummer', 'A', 'S'),
    ('I23', 'Moderne enringsstempel med postnummer', 'A', 'S'),
    ('I24', 'Moderne enringsstempel med postnummer', 'A', 'S'),
    ('III', 'Treringsstempel', 'L', 'S'),
    ('IV', 'Fireringsstempel', 'L', 'S'),
    ('KPH', 'Kronet posthornstempel', 'L', 'S'),
    ('O', 'Oblatstempel', 'L', 'S'),
    ('T', 'Turstempler', 'L', 'S'),
    ('HJ', 'Hjelpestempler', 'L', 'S'),
    ('TMS', 'Maskinstempler', 'A', 'S'),
    ('I10', 'Ministempler', 'A', 'S'),
    ('I12', 'Ministempler', 'A', 'S'),
    ('PPS', 'Pakkepoststempler', 'A', 'S'),
    ('RIS', 'Riststempler', 'L', 'S'),
    ('KS', 'Kvitteringsstempler', 'L', 'S'),
    ('MOS', 'Motivstempler', 'L', 'S'),
    ('KVI', 'Kvitteringsstempler', 'L', 'S'),
    ('OBL', 'Oblatstempel', 'L', 'S'),
    ('TUR', 'Turstempler', 'L', 'S'),
    ('MINI', 'Ministempler', 'A', 'S'),
    ('PRI', 'Private stempler', 'L', 'S'),
    ('HÅND', 'Håndannulleringer', 'L', 'S')
ON CONFLICT (hovedstempeltype) DO NOTHING;
