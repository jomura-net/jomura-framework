#if CODE_ANALYSIS
using System.Diagnostics.CodeAnalysis;

/*
 * �R�[�h��͂̎w�E����
 */

// "Jomura"�̃X�y���`�F�b�N
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Jomura")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "namespace", Target = "Jomura.Data", MessageId = "Jomura")]

// "DAC"�̃X�y���`�F�b�N
[module: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Scope = "type", Target = "Jomura.Data.AbstractDAC", MessageId = "DAC")]

// SQL���O�o�͂�����Ȃ��Ă��A�������s
[module: SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Scope = "member", Target = "Jomura.Data.AbstractDAC.#LogSql(System.Data.IDbCommand)")]

#endif
