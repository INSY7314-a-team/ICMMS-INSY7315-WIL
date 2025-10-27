# ICCMS Design System & Color Theme

## 🎨 **Core Color Palette**

### **Primary Colors**

```css
:root {
  --primary-bg: #1a1b25; /* Dark navy - main background */
  --secondary-bg: #2a2b35; /* Lighter dark - card backgrounds */
  --accent-yellow: #f7ec59; /* Construction yellow - primary CTA */
  --accent-red: #ff5964; /* Alert/overdue color */
  --accent-green: #198754; /* Success/completed color */
  --accent-cyan: #0dcaf0; /* Info/in-progress color */
}
```

### **Text Colors (All White for Readability)**

```css
:root {
  --text-primary: #ffffff; /* Primary text - always white */
  --text-secondary: #ffffff; /* Secondary text - white with different font weight */
  --text-muted: #ffffff; /* Muted text - white with opacity */
  --text-dark: #1a1b25; /* Text on light backgrounds */
}
```

**⚠️ IMPORTANT**: All text must be white (`#FFFFFF`) for optimal readability. Use font weight, size, and opacity variations instead of gray colors for text hierarchy.

## 🎯 **Call-to-Action Hierarchy**

### **1. Primary CTA (Most Important)**

```css
.btn-primary {
  background: #f7ec59;
  color: #1a1b25;
  border: 2px solid #f7ec59;
  border-radius: 25px;
  font-weight: 700;
  padding: 0.75rem 2rem;
}
.btn-primary:hover {
  background: #ffffff;
  color: #1a1b25;
  box-shadow: 0 8px 20px rgba(247, 236, 89, 0.5);
  transform: translateY(-2px);
}
```

### **2. Secondary CTA**

```css
.btn-secondary {
  background: #1a1b25;
  color: #ffffff;
  border: 2px solid #f7ec59;
  border-radius: 25px;
  font-weight: 600;
  padding: 0.75rem 2rem;
}
.btn-secondary:hover {
  background: #f7ec59;
  color: #1a1b25;
  transform: translateY(-2px);
}
```

### **3. Tertiary CTA**

```css
.btn-tertiary {
  background: #2a2b35;
  color: #ffffff;
  border: 1px solid rgba(247, 236, 89, 0.3);
  border-radius: 8px;
  font-weight: 500;
  padding: 0.5rem 1rem;
}
.btn-tertiary:hover {
  background: rgba(247, 236, 89, 0.1);
  border-color: #f7ec59;
  transform: translateY(-1px);
}
```

### **4. Accent CTA (Minimal)**

```css
.btn-accent {
  background: transparent;
  color: #ffffff;
  border: none;
  font-weight: 600;
  text-decoration: underline;
  text-decoration-color: #f7ec59;
}
.btn-accent:hover {
  color: #f7ec59;
  text-decoration-thickness: 2px;
}
```

## 🏗️ **Background Usage**

### **Page Structure**

```css
/* Main page background */
body {
  background-color: #1a1b25;
  color: #ffffff;
}

/* Card backgrounds */
.card {
  background-color: #2a2b35;
  border: 1px solid rgba(247, 236, 89, 0.2);
  border-radius: 8px;
}

/* Section backgrounds */
.section {
  background-color: #1a1b25;
  border: 1px solid rgba(247, 236, 89, 0.2);
  border-radius: 8px;
}

/* Modal backgrounds */
.modal-content {
  background-color: #2a2b35;
  border: 1px solid #f7ec59;
  color: #ffffff;
}
```

## 📊 **Status Color System**

### **Task/Project Status**

```css
.status-pending {
  background: rgba(108, 117, 125, 0.2);
  color: #ffffff;
  border: 1px solid rgba(108, 117, 125, 0.3);
}

.status-in-progress {
  background: rgba(13, 202, 240, 0.2);
  color: #ffffff;
  border: 1px solid rgba(13, 202, 240, 0.3);
}

.status-completed {
  background: rgba(25, 135, 84, 0.2);
  color: #ffffff;
  border: 1px solid rgba(25, 135, 84, 0.3);
}

.status-overdue {
  background: rgba(220, 53, 69, 0.2);
  color: #ffffff;
  border: 1px solid rgba(220, 53, 69, 0.3);
}
```

### **Priority Levels**

```css
.priority-high {
  background: rgba(220, 53, 69, 0.2);
  color: #ffffff;
  border: 1px solid rgba(220, 53, 69, 0.3);
}

.priority-medium {
  background: rgba(255, 193, 7, 0.2);
  color: #ffffff;
  border: 1px solid rgba(255, 193, 7, 0.3);
}

.priority-low {
  background: rgba(25, 135, 84, 0.2);
  color: #ffffff;
  border: 1px solid rgba(25, 135, 84, 0.3);
}
```

## 🔲 **Border Standards**

### **Border Colors**

```css
:root {
  --border-primary: rgba(247, 236, 89, 0.2); /* Default borders */
  --border-active: #f7ec59; /* Active/focus borders */
  --border-construction: repeating-linear-gradient(
    45deg,
    #1a1b25,
    #1a1b25 8px,
    #f7ec59 8px,
    #f7ec59 16px
  ); /* Construction theme borders */
}
```

### **Border Usage**

```css
/* Default card borders */
.card {
  border: 1px solid rgba(247, 236, 89, 0.2);
  border-radius: 8px;
}

/* Active/focus states */
.card:focus,
.card.active {
  border: 2px solid #f7ec59;
  box-shadow: 0 0 0 3px rgba(247, 236, 89, 0.2);
}

/* Construction theme borders */
.construction-border {
  border: 3px solid;
  border-image: var(--border-construction) 1;
}
```

## 📝 **Typography Standards**

### **Text Hierarchy (All White)**

```css
.text-primary {
  color: #ffffff;
  font-weight: 600;
  font-size: 1.1rem;
}

.text-secondary {
  color: #ffffff;
  font-weight: 400;
  font-size: 0.9rem;
  opacity: 0.9;
}

.text-muted {
  color: #ffffff;
  font-weight: 300;
  font-size: 0.8rem;
  opacity: 0.7;
}

.text-small {
  color: #ffffff;
  font-weight: 400;
  font-size: 0.75rem;
  opacity: 0.8;
}
```

### **Font Weights for Hierarchy**

- **Bold (700)**: Headers, important labels
- **Semi-bold (600)**: Subheaders, primary text
- **Regular (400)**: Body text, descriptions
- **Light (300)**: Muted text, captions

## 🎨 **Special Design Elements**

### **Construction Theme**

```css
.construction-border {
  border: 3px solid;
  border-image: repeating-linear-gradient(
      45deg,
      #1a1b25,
      #1a1b25 8px,
      #f7ec59 8px,
      #f7ec59 16px
    ) 1;
}

.construction-border-thin {
  border: 2px solid;
  border-image: repeating-linear-gradient(
      45deg,
      #1a1b25,
      #1a1b25 6px,
      #f7ec59 6px,
      #f7ec59 12px
    ) 1;
}
```

### **Progress Bars**

```css
.progress-bar {
  background: rgba(255, 255, 255, 0.1);
  border-radius: 4px;
  height: 8px;
}

.progress-fill {
  background: #f7ec59;
  border-radius: 4px;
  transition: width 0.3s ease;
}

.progress-fill.success {
  background: #198754;
}
```

### **Shadows & Effects**

```css
.card-shadow {
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.2);
}

.card-shadow-hover {
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
}

.yellow-glow {
  box-shadow: 0 0 12px rgba(247, 236, 89, 0.4);
}

.yellow-glow-strong {
  box-shadow: 0 0 20px rgba(247, 236, 89, 0.6);
}
```

## 📱 **Responsive Design**

### **Breakpoints**

```css
/* Mobile */
@media (max-width: 768px) {
  .container {
    padding: 1rem;
  }

  .card {
    margin-bottom: 1rem;
  }
}

/* Tablet */
@media (min-width: 769px) and (max-width: 1024px) {
  .container {
    padding: 1.5rem;
  }
}

/* Desktop */
@media (min-width: 1025px) {
  .container {
    padding: 2rem;
  }
}
```

## 🎯 **Component Standards**

### **Cards**

```css
.card {
  background: #2a2b35;
  border: 1px solid rgba(247, 236, 89, 0.2);
  border-radius: 8px;
  padding: 1.5rem;
  color: #ffffff;
  transition: all 0.3s ease;
}

.card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
  border-color: #f7ec59;
}
```

### **Buttons**

```css
.btn {
  border-radius: 25px;
  font-weight: 600;
  padding: 0.75rem 2rem;
  transition: all 0.3s ease;
  border: none;
  cursor: pointer;
}

.btn:hover {
  transform: translateY(-2px);
}
```

### **Form Elements**

```css
.form-control {
  background: #2a2b35;
  border: 1px solid rgba(247, 236, 89, 0.2);
  color: #ffffff;
  border-radius: 6px;
  padding: 0.75rem 1rem;
}

.form-control:focus {
  border-color: #f7ec59;
  box-shadow: 0 0 0 3px rgba(247, 236, 89, 0.2);
  background: #1a1b25;
}

.form-control::placeholder {
  color: rgba(255, 255, 255, 0.6);
}
```

## ⚠️ **Important Guidelines**

### **Text Readability**

- **NEVER use gray text** - always use white (`#FFFFFF`)
- Use font weight, size, and opacity for text hierarchy
- Minimum contrast ratio: 4.5:1 for normal text, 3:1 for large text

### **Color Consistency**

- Use only the defined color variables
- Don't create new colors without updating this document
- Test all color combinations for accessibility

### **Component Consistency**

- All cards should use the same border radius (8px)
- All buttons should use the same border radius (25px)
- Maintain consistent spacing using the defined padding/margin values

### **Accessibility**

- All interactive elements must have focus states
- Use sufficient color contrast
- Provide alternative text for icons and images
- Ensure keyboard navigation works properly

## 🔧 **Implementation Notes**

### **CSS Variables Usage**

Always use CSS variables for colors:

```css
/* ✅ Correct */
background: var(--primary-bg);
color: var(--text-primary);

/* ❌ Incorrect */
background: #1a1b25;
color: #ffffff;
```

### **Component Updates**

When updating components:

1. Check this document for color standards
2. Ensure all text is white
3. Use consistent border radius and spacing
4. Test accessibility and readability
5. Update this document if new patterns are introduced

---

**Last Updated**: [2025/10/24]
**Version**: 1.0
**Maintainer**: Development Team
