#if CODE_ANALYSIS
using System.Diagnostics.CodeAnalysis;

/*
 * �R�[�h��͂̎w�E����
 */

// TryParse�̖߂�l�͕s�v�B
[module: SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Scope = "member", Target = "LogService.RemoteLoggingService.#OnStart(System.String[])", MessageId = "System.Int32.TryParse(System.String,System.Int32@)")]

// OnStop()�ł͑S�Ă̗�O�𖳎��B
[module: SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member", Target = "LogService.RemoteLoggingService.#OnStop()")]

#endif
