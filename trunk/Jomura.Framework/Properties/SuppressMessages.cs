#if CODE_ANALYSIS
using System.Diagnostics.CodeAnalysis;

/*
 * �R�[�h��͂̎w�E����
 */

// "Jomura"�̃X�y���`�F�b�N
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Jomura")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "namespace", Target = "Jomura.Data", MessageId = "Jomura")]

//��������Type�����Ȃ��Ă��A���O��Ԃ�ێ�
[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Jomura")]
[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Jomura.Diagnostics.Web")]
[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Jomura.Data")]
[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Jomura.IO")]


// Data/AbstractDAC.cs

// "DAC"�̃X�y���`�F�b�N
[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Scope = "type", Target = "Jomura.Data.AbstractDAC", MessageId = "DAC")]

// SQL���O�o�͂�����Ȃ��Ă��A�������s
[module: SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member", Target = "Jomura.Data.AbstractDAC.#LogSql(System.Data.IDbCommand)")]


// ExUriBuilder.cs

// Uri�^�ł͂Ȃ��A���flex�ɁAstring�^�𗘗p����B
[module: SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Scope = "member", Target = "Jomura.ExUriBuilder.#AddQueryStringToUri(System.String,System.String,System.String)")]
[module: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "Jomura.ExUriBuilder.#AddQueryStringToUri(System.String,System.String,System.String)", MessageId = "string")]
[module: SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Scope = "member", Target = "Jomura.ExUriBuilder.#AddQueryStringToUri(System.String,System.String,System.String)", MessageId = "0#")]
[module: SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Scope = "member", Target = "Jomura.ExUriBuilder.#AddQueryStringsToUri(System.String,System.Collections.Specialized.NameValueCollection)")]
[module: SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", Scope = "member", Target = "Jomura.ExUriBuilder.#AddQueryStringsToUri(System.String,System.Collections.Specialized.NameValueCollection)", MessageId = "string")]
[module: SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Scope = "member", Target = "Jomura.ExUriBuilder.#AddQueryStringsToUri(System.String,System.Collections.Specialized.NameValueCollection)", MessageId = "0#")]
[module: SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads", Scope = "member", Target = "Jomura.ExUriBuilder.#.ctor(System.String)")]
[module: SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings", Scope = "member", Target = "Jomura.ExUriBuilder.#.ctor(System.String)")]

#endif