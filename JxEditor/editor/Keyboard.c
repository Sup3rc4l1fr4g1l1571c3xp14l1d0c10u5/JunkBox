/*
 * ���z�L�[�{�[�h
 */

#include "Keyboard.h"
#include "../pico/pico.memory.h"

#define KEYCODEBUFSIZE 16 //�L�[�R�[�h�o�b�t�@�̃T�C�Y

 // �O���[�o���ϐ���`
static CTRLKEY_FLAG volatile ps2shiftkey_a; //�V�t�g�A�R���g���[���L�[���̏��

typedef struct {
	CTRLKEY_FLAG	ctrlkey;
	uint8_t			keycode;
	uint8_t			reserved;
} key_buffer_entry_t;

static key_buffer_entry_t keycodebuf[KEYCODEBUFSIZE]; //�L�[�R�[�h�o�b�t�@
static key_buffer_entry_t *keycodebufp1; //�L�[�R�[�h�������ݐ擪�|�C���^
static key_buffer_entry_t *keycodebufp2; //�L�[�R�[�h�ǂݏo���擪�|�C���^
static uint8_t ps2keystatus[256]; // ���z�R�[�h�ɑ�������L�[�̏�ԁiOn�̎�1�j

// 
static CTRLKEY_FLAG	currentCtrlKeys;
static uint8_t      currentVKeyCode;
static uint8_t		currentAsciiCode;

static const uint8_t vk2asc1[] = {
	// ���z�L�[�R�[�h����ASCII�R�[�h�ւ̕ϊ��e�[�u���iSHIFT�Ȃ��j
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	' ',0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	'0','1','2','3','4','5','6','7','8','9',0,0,0,0,0,0,
	0,'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o',
	'p','q','r','s','t','u','v','w','x','y','z',0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,'*','+',0,'-',0,'/',
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,':',';',',','-','.','/',
	'@',0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,'[','\\',']','^',0,
	0,0,'\\',0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
};
static const uint8_t vk2asc2[] = {
	// ���z�L�[�R�[�h����ASCII�R�[�h�ւ̕ϊ��e�[�u���iSHIFT����j
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	' ',0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,'!',0x22,'#','$','%','&',0x27,'(',')',0,0,0,0,0,0,
	0,'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
	'P','Q','R','S','T','U','V','W','X','Y','Z',0,0,0,0,0,
	'0','1','2','3','4','5','6','7','8','9','*','+',0,'-','.','/',
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,'*','+','<','=','>','?',
	'`',0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,'{','|','}','~',0,
	0,0,'_',0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
};

static void UpdateCtrlKeyState(unsigned char vk, unsigned char breakflag) {
	// SHIFT,ALT,CTRL,Win�L�[�̉�����Ԃ��X�V
	unsigned short k = 0;
	switch (vk) {
		case VKEY_SHIFT:
		case VKEY_LSHIFT:
			k = CHK_SHIFT_L;
			break;
		case VKEY_RSHIFT:
			k = CHK_SHIFT_R;
			break;
		case VKEY_CONTROL:
		case VKEY_LCONTROL:
			k = CHK_CTRL_L;
			break;
		case VKEY_RCONTROL:
			k = CHK_CTRL_R;
			break;
		case VKEY_MENU:
		case VKEY_LMENU:
			k = CHK_ALT_L;
			break;
		case VKEY_RMENU:
			k = CHK_ALT_R;
			break;
		case VKEY_LWIN:
			k = CHK_WIN_L;
			break;
		case VKEY_RWIN:
			k = CHK_WIN_R;
			break;
	}
	if (breakflag) {
		ps2shiftkey_a &= ~k;
	}
	else {
		ps2shiftkey_a |= k;
	}
}

// NumLock,CapsLock,ScrollLock�̏�ԍX�V
void UpdateLockKeyState(unsigned char vk) {
	switch (vk) {
	case VKEY_SCROLL:
		ps2shiftkey_a ^= CHK_SCRLK;
		break;
	case VKEY_NUMLOCK:
		ps2shiftkey_a ^= CHK_NUMLK;
		break;
	case VKEY_CAPITAL:
		if ((ps2shiftkey_a & CHK_SHIFT) == 0) return;
		ps2shiftkey_a ^= CHK_CAPSLK;
		break;
	default:
		return;
	}
}
static bool_t isShiftkey(unsigned char vk) {
	// SHIFT,ALT,WIN,CTRL�������ꂽ���̃`�F�b�N
	switch (vk) {
	case VKEY_SHIFT:
	case VKEY_LSHIFT:
	case VKEY_RSHIFT:
	case VKEY_CONTROL:
	case VKEY_LCONTROL:
	case VKEY_RCONTROL:
	case VKEY_MENU:
	case VKEY_LMENU:
	case VKEY_RMENU:
	case VKEY_LWIN:
	case VKEY_RWIN:
		return true;
	default:
		return false;
	}
}
static bool_t isLockkey(unsigned char vk) {
	// NumLock,SCRLock,CapsLock�������ꂽ���̃`�F�b�N�i�����ꂽ�ꍇ-1��Ԃ��j
	switch (vk) {
	case VKEY_SCROLL:
	case VKEY_NUMLOCK:
	case VKEY_CAPITAL:
		return true;
	default:
		return false;
	}
}

void Keyboard_PushKeyStatus(uint8_t vk, bool_t breakflag) {
	if (isShiftkey(vk)) {
		if (breakflag == false && ps2keystatus[vk]) return; //�L�[���s�[�g�̏ꍇ�A����
		UpdateCtrlKeyState(vk, breakflag); //SHIFT�n�L�[�̃t���O����
	}
	else if (breakflag == false && isLockkey(vk)) {
		if (ps2keystatus[vk]) return; //�L�[���s�[�g�̏ꍇ�A����
		UpdateLockKeyState(vk); //NumLock�ACapsLock�AScrollLock���]����
	}
	//�L�[�R�[�h�ɑ΂��鉟����Ԕz����X�V
	if (breakflag) {
		ps2keystatus[vk] = 0;
		return;
	}
	ps2keystatus[vk] = 1;

	if ((keycodebufp1 + 1 == keycodebufp2) ||
		(keycodebufp1 == keycodebuf + KEYCODEBUFSIZE - 1) && (keycodebufp2 == keycodebuf)) {
		return; //�o�b�t�@�������ς��̏ꍇ����
	}
	keycodebufp1->keycode = vk;
	keycodebufp1->ctrlkey = ps2shiftkey_a;
	keycodebufp1++;
	if (keycodebufp1 == keycodebuf + KEYCODEBUFSIZE) {
		keycodebufp1 = keycodebuf;
	}
}

void Keyboard_Initialize(void)
{
	// �L�[�{�[�h�V�X�e��������
	keycodebufp1 = keycodebuf;
	keycodebufp2 = keycodebuf;
	ps2shiftkey_a = CHK_NUMLK; // NumLock ������Ԃ�ON�Ƃ���

	//�S�L�[���������
	Memory.Fill.Uint8(ps2keystatus, 0, sizeof(ps2keystatus));

}

// �L�[�o�b�t�@����L�[����ǂݎ��
// ������Ă��Ȃ����false��Ԃ�
bool_t Keyboard_ReadKey(void) {
	unsigned char k2;

	currentAsciiCode = 0x00;
	currentVKeyCode = 0x0000;
	currentCtrlKeys = CHK_NONE;

	if (keycodebufp1 == keycodebufp2) {
		return false;
	}
	key_buffer_entry_t k = *keycodebufp2++;
	currentVKeyCode = k.keycode;
	currentCtrlKeys = k.ctrlkey;

	if (keycodebufp2 == keycodebuf + KEYCODEBUFSIZE) {
		keycodebufp2 = keycodebuf;
	}

	if (k.ctrlkey & (CHK_CTRL | CHK_ALT | CHK_WIN)) {
		return true;
	}

	if (k.keycode >= 'A' && k.keycode <= 'Z') {
		if (((k.ctrlkey & CHK_SHIFT) != 0) != ((k.ctrlkey & CHK_CAPSLK) != 0)) {
			//SHIFT�܂���CapsLock�i�����ł͂Ȃ��j
			k2 = vk2asc2[k.keycode];
		}
		else {
			k2 = vk2asc1[k.keycode];
		}
	}
	else if (k.keycode >= VKEY_NUMPAD0 && k.keycode <= VKEY_DIVIDE) {
		//�e���L�[�֘A
		if ((k.ctrlkey & (CHK_SHIFT | CHK_NUMLK)) == CHK_NUMLK) {
			//NumLock�iSHIFT�{NumLock�͖����j
			k2 = vk2asc2[k.keycode];
		}
		else {
			k2 = vk2asc1[k.keycode];
		}
	}
	else {
		if (k.ctrlkey & CHK_SHIFT) {
			k2 = vk2asc2[k.keycode];
		}
		else {
			k2 = vk2asc1[k.keycode];
		}
	}
	currentAsciiCode = k2;
	return true;
}

CTRLKEY_FLAG Keyboard_GetCurrentCtrlKeys(void)
{
	return currentCtrlKeys;
}

uint8_t Keyboard_GetCurrentVKeyCode(void)
{
	return currentVKeyCode;
}

uint8_t Keyboard_GetCurrentAsciiCode(void)
{
	return currentAsciiCode;
}
