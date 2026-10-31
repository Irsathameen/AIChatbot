-- Table: public.documentchunks

-- DROP TABLE IF EXISTS public.documentchunks;

CREATE TABLE IF NOT EXISTS public.documentchunks
(
    id uuid NOT NULL,
    content text COLLATE pg_catalog."default" NOT NULL,
    metadata jsonb,
    embedding vector(768),
    owner_id uuid NOT NULL,
    CONSTRAINT documentchunks_pkey PRIMARY KEY (id)
);

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.documentchunks
    OWNER to postgres;
-- Index: documentchunks_embedding_idx

-- DROP INDEX IF EXISTS public.documentchunks_embedding_idx;

CREATE INDEX IF NOT EXISTS documentchunks_embedding_idx
    ON public.documentchunks USING ivfflat
    (embedding)
    WITH (lists=100)
    TABLESPACE pg_default;

-------------------------------------------

 CREATE TABLE IF NOT EXISTS public.users
 (
     id uuid PRIMARY KEY,
     email varchar(320) NOT NULL UNIQUE,
     password_hash text NOT NULL,
     created_at timestamptz NOT NULL DEFAULT now()
 );

----------------------------------------------
ALTER TABLE public.documentchunks
    ADD COLUMN IF NOT EXISTS owner_id uuid;

CREATE INDEX IF NOT EXISTS documentchunks_owner_id_idx
    ON public.documentchunks (owner_id);
	
	
	