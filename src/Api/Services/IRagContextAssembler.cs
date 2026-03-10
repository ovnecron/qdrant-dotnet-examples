namespace Api.Services;

internal interface IRagContextAssembler
{
    RagContext Assemble(TextRetrievalResult retrieval);
}
