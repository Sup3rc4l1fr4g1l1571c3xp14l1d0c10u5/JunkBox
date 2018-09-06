// �L�[���́A�J�[�\���\���֘A�@�\ by K.Tanaka
// PS/2�L�[�{�[�h���̓V�X�e���A�J���[�e�L�X�g�o�̓V�X�e�����p

#include "TextVRAM.h"
#include "Keyboard.h"
#include "KeyInput.h"

#include "../arch/arch.h"

//static unsigned short lineinputbuf[256]; //lineinput�֐��p�ꎞ�o�b�t�@
static bool_t insertmode; //�}�����[�h�F1�A�㏑�����[�h�F0

bool_t KeyInput_GetInsertMode(void)
{
	return insertmode;
}

void KeyInput_SetInsertMode(bool_t mode)
{
	insertmode = mode;
}

// �L�[�{�[�h����P�L�[���͓ǂݎ��A�A�X�L�[�L�[�𓾂�
uint8_t KeyInput_getch(void) {
	while (1) {
		WaitMS(1);
		if (Keyboard_ReadKey() && Keyboard_GetCurrentVKeyCode() != 0) {
			return Keyboard_GetCurrentAsciiCode();
		}
	}
}

// �J�[�\���\�����Ȃ���L�[�{�[�h����ʏ핶���L�[�̓��͑҂����A���͂��ꂽ������\���i�G�R�[����j
// �߂�l ���͂��ꂽ������ASCII�R�[�h
uint8_t KeyInput_getchar(void) {
	unsigned char k;
	while (1) {
		WaitMS(1);
		if (Keyboard_ReadKey() && (k = Keyboard_GetCurrentAsciiCode()) != 0) {
			break;
		}
	}
	TextVRAM.putch(k);
	return k;
}



/**
 * @brief �L�[���͂��ĕ�����z��s�Ɋi�[
 * @param str �������������̓��͌��ʎ擾�z��(������������g�p���Ȃ��ꍇ��*str=0)
 * @param sz �o�b�t�@�T�C�Y
 * @retval true: Enter�ŏI��(OK)�Afalse:ESC�ŏI��(�L�����Z��)
 * �J�[�\���ʒu��setcursor�֐��Ŏw�肵�Ă���
 */
bool_t KeyInput_gets(char16_t *str, size32_t sz) {
	if (sz == 0) {
		return false;
	}
	size32_t len, cur = 0;
	for (len=0; str[len] != '\0' && len < sz; len++) { /* loop */ }

	screen_pos_t baseCursorPos;
	TextVRAM.GetCursorPosition(&baseCursorPos);

	if (len > 0) {
		TextVRAM.puts(str); //����������\��
	}
	for (;;) {
		uint8_t k1 = KeyInput_getch();				// �A�X�L�[�R�[�h���擾
		uint8_t k2 = Keyboard_GetCurrentVKeyCode(); // ���z�L�[�R�[�h�擾
		if (k1) {
			//�ʏ핶���̏ꍇ
			if (insertmode || str[cur + 1] == L'\0') {
				//�}�����[�h�܂��͍Ō���̏ꍇ
				if (len+1 > sz) continue; //���͕������ő�l�̏ꍇ�͖���
				for (size32_t i = len; i > cur; i--) {
					str[i + 1] = str[i]; // �J�[�\���ʒu���������Â��炷
				}
				len++;
			}
			str[cur] = k1; //���͕�����ǉ�
			TextVRAM.puts(&str[cur]); //���͕����ȍ~��\��
			cur++;
			{
				screen_pos_t vcp = baseCursorPos;
				TextVRAM.CalcCursorPosition(str,str+cur,&vcp, VWIDTH_X);
				TextVRAM.SetCursorPosition(&vcp);
			}
		}
		else {
			switch (k2) {
				//���䕶���̏ꍇ
			case VKEY_LEFT:
			case VKEY_NUMPAD4:
				//�����L�[
				if (cur > 0) {
					cur--;
					{
						screen_pos_t vcp = baseCursorPos;
						TextVRAM.CalcCursorPosition(str, str + cur, &vcp, VWIDTH_X);
						TextVRAM.SetCursorPosition(&vcp);
					}
				}
				break;
			case VKEY_RIGHT:
			case VKEY_NUMPAD6:
				//�E���L�[
				if (cur != len) {
					cur++;
					{
						screen_pos_t vcp = baseCursorPos;
						TextVRAM.CalcCursorPosition(str, str + cur, &vcp, VWIDTH_X);
						TextVRAM.SetCursorPosition(&vcp);
					}
				}
				break;
			case VKEY_RETURN: //Enter�L�[
			case VKEY_SEPARATOR: //�e���L�[��Enter
				//�I��
				//TextVRAM.putch('\n');
				return true;
			case VKEY_HOME:
			case VKEY_NUMPAD7:
				//Home�L�[�A������擪�ɃJ�[�\���ړ�
				cur = 0;
				TextVRAM.SetCursorPosition(&baseCursorPos);
				break;
			case VKEY_END:
			case VKEY_NUMPAD1:
				//End�L�[�A������Ō���ɃJ�[�\���ړ�
				cur = len;
				{
					screen_pos_t vcp = baseCursorPos;
					TextVRAM.CalcCursorPosition(str, str + cur, &vcp, VWIDTH_X);
					TextVRAM.SetCursorPosition(&vcp);
				}
				break;
			case VKEY_BACK:
				//Back Space�L�[�A1�������Ɉړ���Delete����
				if (cur == 0) break;//�J�[�\�����擪�̏ꍇ�A����
				cur--;
				{
					screen_pos_t vcp = baseCursorPos;
					TextVRAM.CalcCursorPosition(str, str + cur, &vcp, VWIDTH_X);
					TextVRAM.SetCursorPosition(&vcp);
				}
				// fall thought
			case VKEY_DELETE:
			case VKEY_DECIMAL:
				//Delete�L�[�A�J�[�\���ʒu��1�����폜
				if (cur == len) break;//�J�[�\�����Ō���̏ꍇ�A����
				for (size32_t i = cur; i != len; i++) {
					str[i] = str[i + 1];
				}
				len--;
				TextVRAM.puts(str + cur);
				TextVRAM.putch(0);//NULL�����\��
				{
					screen_pos_t vcp = baseCursorPos;
					TextVRAM.CalcCursorPosition(str, str + cur, &vcp, VWIDTH_X);
					TextVRAM.SetCursorPosition(&vcp);
				}
				break;
			case VKEY_INSERT:
			case VKEY_NUMPAD0:
				//Insert�L�[�A�}�����[�h�g�O������
				insertmode = !insertmode;
				break;
			case VKEY_ESCAPE:
				//ESC�L�[�A-1�ŏI��
				return false;
			}
		}
	}
}

const struct KeyInput KeyInput = {
	KeyInput_gets,
	KeyInput_getch,
	KeyInput_getchar,
	KeyInput_GetInsertMode,
	KeyInput_SetInsertMode,
};
