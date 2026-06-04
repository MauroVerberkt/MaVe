; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

 Rule ID | Category | Severity | Notes                                                                    
---------|----------|----------|--------------------------------------------------------------------------
 BR001   | Usage    | Error    | Business rule key not found                                              
 BR002   | Usage    | Error    | Missing ImplementsBusinessRule attribute when enforceValidation is true  
 BR003   | Usage    | Warning  | Missing ImplementsBusinessRule attribute when enforceValidation is false 
 BR004   | Usage    | Warning  | Throwing BusinessRule exception without ImplementsBusinessRule attribute 
