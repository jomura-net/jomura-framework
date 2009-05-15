#if CODE_ANALYSIS
using System.Diagnostics.CodeAnalysis;

/*
 * コード解析の指摘解除
 */

// "Jomura"のスペルチェック
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Jomura")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "namespace", Target = "Jomura.Data", MessageId = "Jomura")]

//所属するTypeが少なくても、名前空間を保持
[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Jomura")]
[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Jomura.Data")]
[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Jomura.Data.Util")]
[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Jomura.Diagnostics.Web")]
[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Jomura.Framework")]
[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Jomura.IO")]


// Data/AbstractDAC.cs

// "DAC"のスペルチェック
[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Scope = "type", Target = "Jomura.Data.AbstractDAC", MessageId = "DAC")]

// SQLログ出力での汎用例外のcatchを許可
[module: SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member", Target = "Jomura.Data.AbstractDAC.#LogSql(System.Data.IDbCommand)")]


// Data/Util/DatabaseCopy.cs

//callbak関数での汎用例外のcatchを許可
[module: SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member", Target = "Jomura.Data.Util.DatabaseCopy.#CopyTableCallback(System.IAsyncResult)")]

//"DDL"を許可
[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Scope = "type", Target = "Jomura.Data.Util.DDL", MessageId = "DDL")]


// ExUriBuilder.cs

// Uri型ではなく、よりflexに、string型を利用する。
[module: SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Scope = "member", Target = "Jomura.UriBuilderEx.#AddQueryStringToUri(System.String,System.String,System.String)", MessageId = "0#")]
[module: SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Scope = "member", Target = "Jomura.UriBuilderEx.#AddQueryStringsToUri(System.String,System.Collections.Specialized.NameValueCollection)", MessageId = "0#")]
[module: SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Scope = "member", Target = "Jomura.UriBuilderEx.#AddQueryStringToUri(System.String,System.String,System.String)")]
[module: SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Scope = "member", Target = "Jomura.UriBuilderEx.#AddQueryStringsToUri(System.String,System.Collections.Specialized.NameValueCollection)")]
[module: SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads", Scope = "member", Target = "Jomura.UriBuilderEx.#.ctor(System.String)")]
[module: SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings", Scope = "member", Target = "Jomura.UriBuilderEx.#.ctor(System.String)")]


#endif
