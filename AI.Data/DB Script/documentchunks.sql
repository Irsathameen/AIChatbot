-- Table: public.documentchunks

-- DROP TABLE IF EXISTS public.documentchunks;

CREATE TABLE IF NOT EXISTS public.documentchunks
(
    id uuid NOT NULL,
    content text COLLATE pg_catalog."default" NOT NULL,
    metadata jsonb,
    embedding vector(768),
    CONSTRAINT documentchunks_pkey PRIMARY KEY (id)
)

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