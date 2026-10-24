Here is a simple README document you can use for your Ollama + ASP.NET RAG project.
Ollama Setup Guide
Overview Ollama is a local LLM runtime that lets you run models like gpt-oss:120b and embedding models such as nomic-embed-text on your machine. This project uses:
•	Chat model: gpt-oss:120b
•	Embedding model: nomic-embed-text
•	Remote chat endpoint: https://ollama.com
•	Local embedding endpoint: http://localhost:11434
Requirements
•	Ollama installed on your machine
•	PostgreSQL with pgvector extension enabled
•	.NET 10 project
•	A working Ollama API key for hosted model access
Install Ollama Download from: https://ollama.com/download

After install, verify:
ollama list

Pull Models
ollama pull gpt-oss:120b
ollama pull nomic-embed-text

Check local status
http://localhost:11434/api/tags

Test Local Embedding Model
http://localhost:11434/api/embed -H "Content-Type: application/json" -d "{\"model\":\"nomic-embed-text\",\"input\":\"hello world\"}"

Configuration In appsettings.json:
"Ollama": {
  "Host": "https://ollama.com",
  "API_Key": "YOUR_OLLAMA_API_KEY",
  "ModelId": "gpt-oss:120b",
  "EmbeddingHost": "http://localhost:11434",
  "EmbeddingModel": "nomic-embed-text",
  "EmbeddingDimensions": 768
}

PostgreSQL Setup Enable pgvector:
Step 1
------
CREATE EXTENSION IF NOT EXISTS vector;

Step 2
------
CREATE TABLE IF NOT EXISTS public.documentchunks
(
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    content text NOT NULL,
    metadata jsonb,
    embedding vector(768),
    CONSTRAINT documentchunks_pkey PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS documentchunks_embedding_idx
ON public.documentchunks
USING hnsw (embedding vector_cosine_ops);
----------------------------


Project Flow
1.	Upload a PDF file
2.	Extract text from the PDF
3.	Split text into chunks
4.	Generate embeddings using nomic-embed-text
5.	Store chunks + embeddings in PostgreSQL
6.	User asks a question
7.	Generate embedding for the question
8.	Search top matching chunks using vector similarity
9.	Pass retrieved context to gpt-oss:120b
10.	Return answer with source snippets