#if CODE_ANALYSIS
using System.Diagnostics.CodeAnalysis;

/*
 * コード解析の指摘解除
 */

// TryParseの戻り値は不要。
[module: SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Scope = "member", Target = "LogService.RemoteLoggingService.#OnStart(System.String[])", MessageId = "System.Int32.TryParse(System.String,System.Int32@)")]

// OnStop()では全ての例外を無視。
[module: SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member", Target = "LogService.RemoteLoggingService.#OnStop()")]

#endif
