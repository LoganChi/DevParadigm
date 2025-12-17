using System.Collections.Generic;

namespace DevParadigm.Example.DTOs.Output;

public class CreateBatchOrderOutput
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
